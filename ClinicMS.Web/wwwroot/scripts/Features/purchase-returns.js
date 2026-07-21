var returns = (typeof PURCHASE_RETURNS_DATA !== 'undefined' ? PURCHASE_RETURNS_DATA : []);
var receivedOrders = (typeof RECEIVED_PURCHASE_ORDERS_DATA !== 'undefined' ? RECEIVED_PURCHASE_ORDERS_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '';

function filtered() {
    var q = searchQuery.toLowerCase();
    return returns.filter(function (r) {
        return !q || r.poNumber.toLowerCase().includes(q) || r.supplierName.toLowerCase().includes(q);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('returnsTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (r, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(r.poNumber)}</span></td>
            <td>${escapeHtml(r.supplierName)}</td>
            <td style="color:#64748b;">${new Date(r.returnDate).toLocaleDateString()}</td>
            <td style="font-weight:700;color:#dc2626;">${r.totalAmount.toLocaleString()}</td>
            <td style="color:#64748b;">${escapeHtml(r.reason || '—')}</td>
        </tr>`; }).join('') : '<tr><td colspan="6" class="text-center py-4 text-muted">No purchase returns recorded</td></tr>';
    document.getElementById('pageInfo').textContent = `Showing ${slice.length} of ${total}`;
    var btns = document.getElementById('pageBtns');
    btns.innerHTML = '';
    for (var p = 1; p <= pages; p++) {
        var btn = document.createElement('button');
        btn.className = 'gp-page-btn' + (p === currentPage ? ' active' : '');
        btn.textContent = p;
        btn.onclick = (function (pp) { return function () { currentPage = pp; renderTable(); }; })(p);
        btns.appendChild(btn);
    }
}

// How much of a given SKU on a given PO has already been returned, across all prior returns.
function alreadyReturnedQty(poId, skuId) {
    return returns
        .filter(function (r) { return r.purchaseOrderId === poId; })
        .reduce(function (sum, r) {
            var item = r.items.find(function (i) { return i.productSkuId === skuId; });
            return sum + (item ? item.quantity : 0);
        }, 0);
}

function populateOrderSelect() {
    var sel = document.getElementById('fPurchaseOrder');
    sel.innerHTML = '<option value="">— Select a purchase order —</option>' +
        receivedOrders.map(function (o) { return `<option value="${o.id}">${escapeHtml(o.poNumber)} — ${escapeHtml(o.supplierName)}</option>`; }).join('');
}

function onPurchaseOrderChange() {
    var orderId = parseInt(document.getElementById('fPurchaseOrder').value, 10);
    var section = document.getElementById('returnItemsSection');
    var hint = document.getElementById('noOrderSelectedHint');
    var container = document.getElementById('returnItemsContainer');

    if (!orderId) {
        section.style.display = 'none';
        hint.style.display = '';
        return;
    }

    var order = receivedOrders.find(function (o) { return o.id === orderId; });
    var rows = order.items.map(function (item) {
        var already = alreadyReturnedQty(order.id, item.productSkuId);
        var available = item.quantityReceived - already;
        return `
        <div class="ret-item-row" data-sku="${item.productSkuId}" data-available="${available}" data-default-cost="${item.unitCost}">
            <span>${escapeHtml(item.skuCode)} — ${escapeHtml(item.productName)}</span>
            <span style="color:#64748b;">${available}</span>
            <input type="text" inputmode="numeric" class="form-control" value="0" ${available <= 0 ? 'disabled' : ''} oninput="sanitizeIntField(this)">
            <input type="text" inputmode="decimal" class="form-control" value="${item.unitCost}" ${available <= 0 ? 'disabled' : ''} oninput="sanitizeDecimalField(this)">
        </div>`;
    }).join('');

    container.innerHTML = rows;
    hint.style.display = 'none';
    section.style.display = '';
    recomputeTotal();
}

function sanitizeIntField(input) { input.value = input.value.replace(/[^0-9]/g, ''); recomputeTotal(); }
function sanitizeDecimalField(input) {
    var value = input.value.replace(/[^0-9.]/g, '');
    var firstDot = value.indexOf('.');
    if (firstDot !== -1) value = value.slice(0, firstDot + 1) + value.slice(firstDot + 1).replace(/\./g, '');
    input.value = value;
    recomputeTotal();
}

function recomputeTotal() {
    var rows = document.querySelectorAll('#returnItemsContainer .ret-item-row');
    var total = 0;
    rows.forEach(function (row) {
        var inputs = row.querySelectorAll('input');
        var qty = parseFloat(inputs[0].value) || 0;
        var cost = parseFloat(inputs[1].value) || 0;
        total += qty * cost;
    });
    document.getElementById('returnTotal').textContent = total.toFixed(2);
}

function openModal() {
    populateOrderSelect();
    document.getElementById('fReason').value = '';
    document.getElementById('returnItemsContainer').innerHTML = '';
    document.getElementById('returnItemsSection').style.display = 'none';
    document.getElementById('noOrderSelectedHint').style.display = '';
    new bootstrap.Modal(document.getElementById('returnModal')).show();
}

function saveReturn() {
    var orderId = parseInt(document.getElementById('fPurchaseOrder').value, 10);
    if (!orderId) { toastr.error('Select a purchase order'); return; }

    var rows = document.querySelectorAll('#returnItemsContainer .ret-item-row');
    var items = [];
    for (var i = 0; i < rows.length; i++) {
        var inputs = rows[i].querySelectorAll('input');
        var qty = parseInt(inputs[0].value, 10) || 0;
        if (qty <= 0) continue;

        var available = parseInt(rows[i].getAttribute('data-available'), 10);
        var unitCost = parseFloat(inputs[1].value);
        if (qty > available) { toastr.error('Cannot return more than ' + available + ' units for one of the selected items'); return; }
        if (isNaN(unitCost) || unitCost < 0) { toastr.error('Enter a valid unit cost for every returned item'); return; }

        items.push({ productSkuId: parseInt(rows[i].getAttribute('data-sku'), 10), quantity: qty, unitCost: unitCost });
    }

    if (items.length === 0) { toastr.error('Enter a return quantity for at least one item'); return; }

    var body = { purchaseOrderId: orderId, reason: document.getElementById('fReason').value.trim() || null, items: items };

    fetch('/SupplyChain/CreatePurchaseReturn', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not record purchase return'); return; }
        returns.unshift(result.data);
        toastr.success('Purchase return recorded');
        bootstrap.Modal.getInstance(document.getElementById('returnModal')).hide();
        renderTable();
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
