var skus = (typeof PRODUCT_SKUS_DATA !== 'undefined' ? PRODUCT_SKUS_DATA : []);
var skuProducts = (typeof PRODUCTS_DATA !== 'undefined' ? PRODUCTS_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', stockF = '', editingId = null;

function isDecimalKeyAllowed(e) {
    if (e.ctrlKey || e.metaKey || e.altKey) return true;
    var ch = e.key;
    if (ch.length !== 1) return true;
    if (ch >= '0' && ch <= '9') return true;
    if (ch === '.' && e.target.value.indexOf('.') === -1) return true;
    e.preventDefault();
    return false;
}
function sanitizeDecimal(input) {
    var value = input.value.replace(/[^0-9.]/g, '');
    var firstDot = value.indexOf('.');
    if (firstDot !== -1) value = value.slice(0, firstDot + 1) + value.slice(firstDot + 1).replace(/\./g, '');
    input.value = value;
}
function isIntKeyAllowed(e) {
    if (e.ctrlKey || e.metaKey || e.altKey) return true;
    var ch = e.key;
    if (ch.length !== 1) return true;
    if (ch >= '0' && ch <= '9') return true;
    e.preventDefault();
    return false;
}
function sanitizeInt(input) { input.value = input.value.replace(/[^0-9]/g, ''); }

function filtered() {
    var q = searchQuery.toLowerCase();
    return skus.filter(function (s) {
        var match = !q || s.skuCode.toLowerCase().includes(q) || s.productName.toLowerCase().includes(q);
        var low = stockF !== 'low' || s.stockQuantity <= s.reorderLevel;
        return match && low;
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('skusTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (s, i) {
        var low = s.stockQuantity <= s.reorderLevel;
        return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${s.skuCode}</span></td>
            <td>${s.productName}</td>
            <td style="color:#64748b;">${s.unitName}</td>
            <td>${s.costPrice.toLocaleString()}</td>
            <td style="font-weight:700;color:#0d9488;">${s.sellingPrice.toLocaleString()}</td>
            <td><span class="gp-badge ${low ? 'gp-badge-amber' : 'gp-badge-green'}">${s.stockQuantity}</span></td>
            <td style="color:#64748b;">${s.reorderLevel}</td>
            <td><span class="gp-badge ${s.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${s.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${s.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteSku(${s.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="10" class="text-center py-4 text-muted">No SKUs found</td></tr>';
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

function populateProductSelect(selectedId) {
    var sel = document.getElementById('fProduct');
    sel.innerHTML = skuProducts.map(function (p) {
        return `<option value="${p.id}" ${p.id === selectedId ? 'selected' : ''}>${p.productName}</option>`;
    }).join('');
}

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit SKU' : 'Add SKU';
    var stockInput = document.getElementById('fStockQuantity');
    if (id) {
        var s = skus.find(function (x) { return x.id === id; });
        populateProductSelect(s.productId);
        document.getElementById('fSkuCode').value = s.skuCode;
        document.getElementById('fUnitName').value = s.unitName;
        document.getElementById('fCostPrice').value = s.costPrice;
        document.getElementById('fSellingPrice').value = s.sellingPrice;
        stockInput.value = s.stockQuantity;
        stockInput.disabled = true;
        document.getElementById('fReorderLevel').value = s.reorderLevel;
        document.getElementById('fActive').checked = s.isActive;
    } else {
        populateProductSelect(skuProducts.length ? skuProducts[0].id : null);
        document.getElementById('fSkuCode').value = '';
        document.getElementById('fUnitName').value = '';
        document.getElementById('fCostPrice').value = '';
        document.getElementById('fSellingPrice').value = '';
        stockInput.value = '0';
        stockInput.disabled = false;
        document.getElementById('fReorderLevel').value = '';
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('skuModal')).show();
}

function saveSku() {
    var productId = parseInt(document.getElementById('fProduct').value, 10);
    var skuCode = document.getElementById('fSkuCode').value.trim();
    var unitName = document.getElementById('fUnitName').value.trim();
    var costPrice = parseFloat(document.getElementById('fCostPrice').value);
    var sellingPrice = parseFloat(document.getElementById('fSellingPrice').value);
    var stockQuantity = parseInt(document.getElementById('fStockQuantity').value, 10);
    var reorderLevel = parseInt(document.getElementById('fReorderLevel').value, 10);
    var isActive = document.getElementById('fActive').checked;

    if (!productId) { toastr.error('Product is required'); return; }
    if (!skuCode) { toastr.error('SKU code is required'); return; }
    if (!unitName) { toastr.error('Unit is required'); return; }
    if (isNaN(costPrice) || costPrice < 0) { toastr.error('Enter a valid cost price'); return; }
    if (isNaN(sellingPrice) || sellingPrice < 0) { toastr.error('Enter a valid selling price'); return; }
    if (isNaN(reorderLevel) || reorderLevel < 0) { toastr.error('Enter a valid reorder level'); return; }

    var url, body;
    if (editingId) {
        url = '/SupplyChain/UpdateProductSku?id=' + editingId;
        body = { productId: productId, skuCode: skuCode, unitName: unitName, costPrice: costPrice, sellingPrice: sellingPrice, reorderLevel: reorderLevel, isActive: isActive };
    } else {
        if (isNaN(stockQuantity) || stockQuantity < 0) { toastr.error('Enter a valid opening stock'); return; }
        url = '/SupplyChain/CreateProductSku';
        body = { productId: productId, skuCode: skuCode, unitName: unitName, costPrice: costPrice, sellingPrice: sellingPrice, stockQuantity: stockQuantity, reorderLevel: reorderLevel, isActive: isActive };
    }

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save SKU'); return; }
        if (editingId) {
            var idx = skus.findIndex(function (s) { return s.id === editingId; });
            if (idx >= 0) skus[idx] = result.data;
            toastr.success('SKU updated');
        } else {
            skus.push(result.data);
            toastr.success('SKU added');
        }
        bootstrap.Modal.getInstance(document.getElementById('skuModal')).hide();
        renderTable();
    });
}

function deleteSku(id) {
    if (!confirm('Delete this SKU?')) return;
    fetch('/SupplyChain/DeleteProductSku?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete SKU'); return; }
            skus = skus.filter(function (s) { return s.id !== id; });
            toastr.success('Deleted');
            renderTable();
        });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() { stockF = document.getElementById('stockFilter').value; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
