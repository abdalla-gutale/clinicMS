var orders = (typeof PURCHASE_ORDERS_DATA !== 'undefined' ? PURCHASE_ORDERS_DATA : []);
var poSuppliers = (typeof PO_SUPPLIERS_DATA !== 'undefined' ? PO_SUPPLIERS_DATA : []);
var poSkus = (typeof PO_SKUS_DATA !== 'undefined' ? PO_SKUS_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', statusF = '', itemRowSeq = 0;

var statusBadge = { Draft: 'gp-badge-slate', Ordered: 'gp-badge-amber', Received: 'gp-badge-green', Cancelled: 'gp-badge-red' };

function filtered() {
    var q = searchQuery.toLowerCase();
    return orders.filter(function (o) {
        var match = !q || o.poNumber.toLowerCase().includes(q) || o.supplierName.toLowerCase().includes(q);
        var s = !statusF || o.status === statusF;
        return match && s;
    });
}

function actionButtons(o) {
    var btns = '';
    if (o.status === 'Draft') {
        btns += `<button class="gp-btn-sm gp-btn-order" onclick="changeStatus(${o.id},'Ordered')">Mark Ordered</button>`;
        btns += `<button class="gp-btn-icon gp-btn-delete" onclick="deleteOrder(${o.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>`;
    } else if (o.status === 'Ordered') {
        btns += `<button class="gp-btn-sm gp-btn-receive" onclick="changeStatus(${o.id},'Received')">Mark Received</button>`;
        btns += `<button class="gp-btn-sm gp-btn-cancel" onclick="changeStatus(${o.id},'Cancelled')">Cancel</button>`;
    }
    return `<div class="action-btns">${btns}</div>`;
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('ordersTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (o, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${o.poNumber}</span></td>
            <td>${o.supplierName}</td>
            <td style="color:#64748b;">${new Date(o.orderDate).toLocaleDateString()}</td>
            <td style="color:#64748b;">${o.expectedDeliveryDate ? new Date(o.expectedDeliveryDate).toLocaleDateString() : '—'}</td>
            <td style="font-weight:700;color:#0d9488;">${o.totalAmount.toLocaleString()}</td>
            <td><span class="gp-badge ${statusBadge[o.status] || ''}">${o.status}</span></td>
            <td>${actionButtons(o)}</td>
        </tr>`; }).join('') : '<tr><td colspan="8" class="text-center py-4 text-muted">No purchase orders found</td></tr>';
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

function populateSupplierSelect() {
    var sel = document.getElementById('fSupplier');
    sel.innerHTML = poSuppliers.map(function (s) { return `<option value="${s.id}">${s.supplierName}</option>`; }).join('');
}

function skuOptionsHtml(selectedId) {
    return poSkus.map(function (s) {
        return `<option value="${s.id}" data-cost="${s.costPrice}" ${s.id === selectedId ? 'selected' : ''}>${s.skuCode} — ${s.productName}</option>`;
    }).join('');
}

function addItemRow() {
    var id = 'row' + (itemRowSeq++);
    var row = document.createElement('div');
    row.className = 'po-item-row';
    row.id = id;
    row.innerHTML = `
        <select class="form-select" onchange="onSkuChange('${id}')">${skuOptionsHtml(poSkus.length ? poSkus[0].id : null)}</select>
        <input type="text" inputmode="numeric" class="form-control" value="1" oninput="sanitizeIntField(this); recomputeTotal();">
        <input type="text" inputmode="decimal" class="form-control" oninput="sanitizeDecimalField(this); recomputeTotal();">
        <span class="line-total" style="font-weight:700;">0.00</span>
        <button type="button" class="gp-btn-icon gp-btn-delete" onclick="document.getElementById('${id}').remove(); recomputeTotal();"><i class="ri-close-line"></i></button>`;
    document.getElementById('poItemsContainer').appendChild(row);
    onSkuChange(id);
    initSelect2();
}

function onSkuChange(rowId) {
    var row = document.getElementById(rowId);
    var select = row.querySelector('select');
    var costInput = row.querySelectorAll('input')[1];
    var cost = select.selectedOptions[0] ? select.selectedOptions[0].getAttribute('data-cost') : '0';
    costInput.value = cost;
    recomputeTotal();
}

function sanitizeIntField(input) { input.value = input.value.replace(/[^0-9]/g, ''); recomputeLineTotal(input); }
function sanitizeDecimalField(input) {
    var value = input.value.replace(/[^0-9.]/g, '');
    var firstDot = value.indexOf('.');
    if (firstDot !== -1) value = value.slice(0, firstDot + 1) + value.slice(firstDot + 1).replace(/\./g, '');
    input.value = value;
    recomputeLineTotal(input);
}
function recomputeLineTotal(input) {
    var row = input.closest('.po-item-row');
    var inputs = row.querySelectorAll('input');
    var qty = parseFloat(inputs[0].value) || 0;
    var cost = parseFloat(inputs[1].value) || 0;
    row.querySelector('.line-total').textContent = (qty * cost).toFixed(2);
}

function recomputeTotal() {
    var rows = document.querySelectorAll('#poItemsContainer .po-item-row');
    var total = 0;
    rows.forEach(function (row) {
        var inputs = row.querySelectorAll('input');
        var qty = parseFloat(inputs[0].value) || 0;
        var cost = parseFloat(inputs[1].value) || 0;
        var line = qty * cost;
        row.querySelector('.line-total').textContent = line.toFixed(2);
        total += line;
    });
    document.getElementById('poTotal').textContent = total.toFixed(2);
}

function openModal() {
    populateSupplierSelect();
    document.getElementById('fExpectedDelivery').value = '';
    document.getElementById('fNotes').value = '';
    document.getElementById('poItemsContainer').innerHTML = '';
    itemRowSeq = 0;
    addItemRow();
    new bootstrap.Modal(document.getElementById('orderModal')).show();
}

function saveOrder() {
    var supplierId = parseInt(document.getElementById('fSupplier').value, 10);
    var expectedDelivery = document.getElementById('fExpectedDelivery').value;
    var notes = document.getElementById('fNotes').value.trim();

    if (!supplierId) { toastr.error('Supplier is required'); return; }

    var rows = document.querySelectorAll('#poItemsContainer .po-item-row');
    if (rows.length === 0) { toastr.error('Add at least one item'); return; }

    var items = [];
    for (var i = 0; i < rows.length; i++) {
        var select = rows[i].querySelector('select');
        var inputs = rows[i].querySelectorAll('input');
        var productSkuId = parseInt(select.value, 10);
        var qty = parseInt(inputs[0].value, 10);
        var unitCost = parseFloat(inputs[1].value);
        if (!productSkuId) { toastr.error('Select a SKU for every item row'); return; }
        if (isNaN(qty) || qty <= 0) { toastr.error('Enter a valid quantity for every item row'); return; }
        if (isNaN(unitCost) || unitCost < 0) { toastr.error('Enter a valid unit cost for every item row'); return; }
        items.push({ productSkuId: productSkuId, quantityOrdered: qty, unitCost: unitCost });
    }

    var body = { supplierId: supplierId, expectedDeliveryDate: expectedDelivery || null, notes: notes || null, items: items };

    fetch('/SupplyChain/CreatePurchaseOrder', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not create purchase order'); return; }
        orders.unshift(result.data);
        toastr.success('Purchase order created');
        bootstrap.Modal.getInstance(document.getElementById('orderModal')).hide();
        renderTable();
    });
}

function changeStatus(id, status) {
    var verb = status === 'Received' ? 'mark this order as received (this will add stock)' : status === 'Cancelled' ? 'cancel this order' : 'mark this order as ordered';
    if (!confirm('Are you sure you want to ' + verb + '?')) return;

    fetch('/SupplyChain/UpdatePurchaseOrderStatus?id=' + id, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status: status })
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not update status'); return; }
        var idx = orders.findIndex(function (o) { return o.id === id; });
        if (idx >= 0) orders[idx] = result.data;
        toastr.success('Status updated');
        renderTable();
    });
}

function deleteOrder(id) {
    if (!confirm('Delete this draft purchase order?')) return;
    fetch('/SupplyChain/DeletePurchaseOrder?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete purchase order'); return; }
            orders = orders.filter(function (o) { return o.id !== id; });
            toastr.success('Deleted');
            renderTable();
        });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() { statusF = document.getElementById('statusFilter').value; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
