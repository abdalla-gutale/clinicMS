var products = (typeof PRODUCTS_DATA !== 'undefined' ? PRODUCTS_DATA : []);
var productCategories = (typeof PRODUCT_CATEGORIES_DATA !== 'undefined' ? PRODUCT_CATEGORIES_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', categoryF = '', statusF = '', editingId = null;

function filtered() {
    return products.filter(function (p) {
        var q = searchQuery.toLowerCase();
        var match = !q || p.productName.toLowerCase().includes(q) || p.productCategoryName.toLowerCase().includes(q);
        var c = !categoryF || p.productCategoryName === categoryF;
        var st = !statusF || (p.isActive ? 'Active' : 'Inactive') === statusF;
        return match && c && st;
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('productsTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (p, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(p.productName)}</span></td>
            <td><span class="gp-badge gp-badge-teal">${escapeHtml(p.productCategoryName)}</span></td>
            <td style="color:#64748b;">${escapeHtml(p.description || '')}</td>
            <td><span class="gp-badge ${p.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${p.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${p.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteProduct(${p.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="6" class="text-center py-4 text-muted">No products found</td></tr>';
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

function populateCategoryFilter() {
    var sel = document.getElementById('categoryFilter');
    sel.innerHTML = '<option value="">All Categories</option>' +
        productCategories.map(function (c) { return `<option>${c.categoryName}</option>`; }).join('');
}

function populateCategorySelect(selectedId) {
    var sel = document.getElementById('fCategory');
    sel.innerHTML = productCategories.map(function (c) {
        return `<option value="${c.id}" ${c.id === selectedId ? 'selected' : ''}>${c.categoryName}</option>`;
    }).join('');
}

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Product' : 'Add Product';
    if (id) {
        var p = products.find(function (x) { return x.id === id; });
        populateCategorySelect(p.productCategoryId);
        document.getElementById('fProductName').value = p.productName;
        document.getElementById('fDescription').value = p.description || '';
        document.getElementById('fActive').checked = p.isActive;
    } else {
        populateCategorySelect(productCategories.length ? productCategories[0].id : null);
        document.getElementById('fProductName').value = '';
        document.getElementById('fDescription').value = '';
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('productModal')).show();
}

function saveProduct() {
    var productCategoryId = parseInt(document.getElementById('fCategory').value, 10);
    var productName = document.getElementById('fProductName').value.trim();
    var description = document.getElementById('fDescription').value.trim();
    var isActive = document.getElementById('fActive').checked;

    if (!productCategoryId) { toastr.error('Category is required'); return; }
    if (!productName) { toastr.error('Product name is required'); return; }

    var body = { productCategoryId: productCategoryId, productName: productName, description: description || null, isActive: isActive };
    var url = editingId ? '/SupplyChain/UpdateProduct?id=' + editingId : '/SupplyChain/CreateProduct';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save product'); return; }
        if (editingId) {
            var idx = products.findIndex(function (p) { return p.id === editingId; });
            if (idx >= 0) products[idx] = result.data;
            toastr.success('Product updated');
        } else {
            products.push(result.data);
            toastr.success('Product added');
        }
        bootstrap.Modal.getInstance(document.getElementById('productModal')).hide();
        renderTable();
    });
}

function deleteProduct(id) {
    confirmDelete('This product will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/SupplyChain/DeleteProduct?id=' + id, { method: 'POST' })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete product'); return; }
                products = products.filter(function (p) { return p.id !== id; });
                deletedAlert('Product deleted.');
                renderTable();
            });
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() {
    categoryF = document.getElementById('categoryFilter').value;
    statusF = document.getElementById('statusFilter').value;
    currentPage = 1; renderTable();
}

document.addEventListener('DOMContentLoaded', function () {
    populateCategoryFilter();
    renderTable();
});

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
