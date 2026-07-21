var movements = (typeof STOCK_MOVEMENTS_DATA !== 'undefined' ? STOCK_MOVEMENTS_DATA : []);
var movementSkus = (typeof PRODUCT_SKUS_DATA !== 'undefined' ? PRODUCT_SKUS_DATA : []);

var currentPage = 1, perPage = 10, searchQuery = '', typeF = '';

var typeBadge = { In: 'gp-badge-green', Out: 'gp-badge-red', Adjustment: 'gp-badge-amber' };

function filtered() {
    var q = searchQuery.toLowerCase();
    return movements.filter(function (m) {
        var match = !q || m.skuCode.toLowerCase().includes(q) || m.productName.toLowerCase().includes(q);
        var t = !typeF || m.movementType === typeF;
        return match && t;
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('movementsTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (m, i) {
        var qtyPrefix = m.movementType === 'Out' ? '-' : (m.movementType === 'In' ? '+' : (m.quantity >= 0 ? '+' : ''));
        return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td style="color:#64748b;">${new Date(m.movementDate).toLocaleString()}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(m.skuCode)}</span></td>
            <td>${escapeHtml(m.productName)}</td>
            <td><span class="gp-badge ${typeBadge[m.movementType] || ''}">${m.movementType}</span></td>
            <td style="font-weight:700;">${qtyPrefix}${Math.abs(m.quantity)}</td>
            <td style="color:#64748b;">${escapeHtml(m.referenceId || '')}</td>
            <td style="color:#64748b;">${escapeHtml(m.notes || '')}</td>
        </tr>`; }).join('') : '<tr><td colspan="8" class="text-center py-4 text-muted">No stock movements found</td></tr>';
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

function populateSkuSelect() {
    var sel = document.getElementById('fSku');
    sel.innerHTML = movementSkus.map(function (s) {
        return `<option value="${s.id}">${escapeHtml(s.skuCode)} — ${escapeHtml(s.productName)} (${s.stockQuantity} in stock)</option>`;
    }).join('');
}

function openModal() {
    populateSkuSelect();
    document.getElementById('fMovementType').value = 'In';
    document.getElementById('fQuantity').value = '';
    document.getElementById('fReferenceId').value = '';
    document.getElementById('fNotes').value = '';
    new bootstrap.Modal(document.getElementById('movementModal')).show();
}

function saveMovement() {
    var productSkuId = parseInt(document.getElementById('fSku').value, 10);
    var movementType = document.getElementById('fMovementType').value;
    var quantity = parseInt(document.getElementById('fQuantity').value, 10);
    var referenceId = document.getElementById('fReferenceId').value.trim();
    var notes = document.getElementById('fNotes').value.trim();

    if (!productSkuId) { toastr.error('Product SKU is required'); return; }
    if (isNaN(quantity) || quantity === 0) { toastr.error('Enter a valid quantity'); return; }
    if (movementType !== 'Adjustment' && quantity < 0) { toastr.error('Quantity must be positive for In/Out movements'); return; }

    var body = { productSkuId: productSkuId, movementType: movementType, quantity: quantity, referenceId: referenceId || null, notes: notes || null };

    fetch('/SupplyChain/CreateStockMovement', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not record movement'); return; }
        movements.unshift(result.data);
        var sku = movementSkus.find(function (s) { return s.id === productSkuId; });
        if (sku) {
            var delta = movementType === 'In' ? quantity : movementType === 'Out' ? -quantity : quantity;
            sku.stockQuantity += delta;
        }
        toastr.success('Movement recorded');
        bootstrap.Modal.getInstance(document.getElementById('movementModal')).hide();
        renderTable();
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() { typeF = document.getElementById('typeFilter').value; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
