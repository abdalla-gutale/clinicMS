var refunds = (typeof PRODUCT_REFUNDS_DATA !== 'undefined' ? PRODUCT_REFUNDS_DATA : []);
var currentPage = 1, perPage = 8, searchQuery = '';
var loadedInvoice = null;

function filtered() {
    var q = searchQuery.toLowerCase();
    return refunds.filter(function (r) {
        return !q || r.invoiceNumber.toLowerCase().includes(q) || (r.patientName || '').toLowerCase().includes(q);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('refundsTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (r, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td style="color:#64748b;">${new Date(r.refundDate).toLocaleString()}</td>
            <td><span style="font-weight:700;color:#1e293b;">${r.invoiceNumber}</span></td>
            <td>${r.patientName || '—'}</td>
            <td><span class="gp-badge ${r.refundType === 'Full' ? 'gp-badge-amber' : 'gp-badge-green'}">${r.refundType}</span></td>
            <td style="color:#64748b;">${r.reason || ''}</td>
            <td style="font-weight:700;color:#dc2626;">-${r.totalRefundAmount.toLocaleString()}</td>
        </tr>`; }).join('') : '<tr><td colspan="7" class="text-center py-4 text-muted">No product refunds found</td></tr>';
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

function openModal() {
    loadedInvoice = null;
    document.getElementById('fInvoiceId').value = '';
    document.getElementById('fReason').value = '';
    document.getElementById('invoiceResult').style.display = 'none';
    document.getElementById('saveRefundBtn').disabled = true;
    new bootstrap.Modal(document.getElementById('refundModal')).show();
}

function lookupInvoice() {
    var id = parseInt(document.getElementById('fInvoiceId').value, 10);
    if (!id) { toastr.error('Enter a valid invoice number'); return; }

    fetch('/Payments/GetInvoice?id=' + id)
        .then(function (res) { return res.json().then(function (data) { return { ok: res.ok, data: data }; }); })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Invoice not found'); return; }
            loadedInvoice = result.data;
            var productItems = loadedInvoice.items.filter(function (it) { return it.itemType === 'Product' && it.productSkuId; });
            if (productItems.length === 0) {
                toastr.error('This invoice has no product items to refund');
                document.getElementById('invoiceResult').style.display = 'none';
                document.getElementById('saveRefundBtn').disabled = true;
                return;
            }

            document.getElementById('invoiceSummaryLine').textContent =
                `${loadedInvoice.invoiceNumber} — ${loadedInvoice.patientName || 'Walk-in'} — ${new Date(loadedInvoice.invoiceDate).toLocaleDateString()}`;

            var container = document.getElementById('refundItemsContainer');
            container.innerHTML = productItems.map(function (it, idx) { return `
                <div class="refund-item-row" data-idx="${idx}">
                    <input type="checkbox" class="form-check-input refund-include" checked onchange="recomputeRefundTotal()">
                    <span>${it.productName} <span style="color:#94a3b8;">(${it.skuCode || ''})</span></span>
                    <input type="text" inputmode="numeric" class="form-control refund-qty" value="${it.quantity}" oninput="sanitizeRefundQty(this, ${it.quantity}); recomputeRefundTotal();">
                    <input type="text" inputmode="decimal" class="form-control refund-price" value="${it.unitPrice}" oninput="sanitizeRefundPrice(this); recomputeRefundTotal();">
                    <input type="checkbox" class="form-check-input refund-restock" checked>
                </div>`; }).join('');

            document.getElementById('invoiceResult').style.display = 'block';
            document.getElementById('saveRefundBtn').disabled = false;
            recomputeRefundTotal();
        });
}

function sanitizeRefundQty(input, max) {
    var value = input.value.replace(/[^0-9]/g, '');
    var num = parseInt(value, 10);
    if (!isNaN(num) && num > max) value = String(max);
    input.value = value;
}
function sanitizeRefundPrice(input) {
    var value = input.value.replace(/[^0-9.]/g, '');
    var firstDot = value.indexOf('.');
    if (firstDot !== -1) value = value.slice(0, firstDot + 1) + value.slice(firstDot + 1).replace(/\./g, '');
    input.value = value;
}

function recomputeRefundTotal() {
    var rows = document.querySelectorAll('#refundItemsContainer .refund-item-row');
    var total = 0;
    rows.forEach(function (row) {
        if (!row.querySelector('.refund-include').checked) return;
        var qty = parseFloat(row.querySelector('.refund-qty').value) || 0;
        var price = parseFloat(row.querySelector('.refund-price').value) || 0;
        total += qty * price;
    });
    document.getElementById('refundTotal').textContent = total.toFixed(2);
}

function saveRefund() {
    if (!loadedInvoice) { toastr.error('Look up an invoice first'); return; }
    var productItems = loadedInvoice.items.filter(function (it) { return it.itemType === 'Product' && it.productSkuId; });
    var rows = document.querySelectorAll('#refundItemsContainer .refund-item-row');

    var items = [];
    var fullRefund = true;
    rows.forEach(function (row) {
        var idx = parseInt(row.getAttribute('data-idx'), 10);
        var invoiceItem = productItems[idx];
        if (!row.querySelector('.refund-include').checked) { fullRefund = false; return; }
        var qty = parseInt(row.querySelector('.refund-qty').value, 10);
        var price = parseFloat(row.querySelector('.refund-price').value);
        if (isNaN(qty) || qty <= 0) { fullRefund = false; return; }
        if (qty < invoiceItem.quantity) fullRefund = false;
        items.push({ productSkuId: invoiceItem.productSkuId, quantity: qty, refundUnitPrice: isNaN(price) ? invoiceItem.unitPrice : price, restockItem: row.querySelector('.refund-restock').checked });
    });

    if (items.length === 0) { toastr.error('Select at least one item to refund'); return; }

    var body = { invoiceId: loadedInvoice.id, refundType: fullRefund ? 'Full' : 'Partial', reason: document.getElementById('fReason').value.trim() || null, items: items };

    fetch('/Payments/CreateProductRefund', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not process refund'); return; }
        refunds.unshift(result.data);
        toastr.success('Refund processed');
        bootstrap.Modal.getInstance(document.getElementById('refundModal')).hide();
        renderTable();
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });
