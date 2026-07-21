var categories = (typeof PRODUCT_CATEGORIES_DATA !== 'undefined' ? PRODUCT_CATEGORIES_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', editingId = null;

function filtered() {
    var q = searchQuery.toLowerCase();
    return categories.filter(function (c) {
        return !q || c.categoryName.toLowerCase().includes(q) || (c.description || '').toLowerCase().includes(q);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('categoriesTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (c, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(c.categoryName)}</span></td>
            <td style="color:#64748b;">${escapeHtml(c.description || '')}</td>
            <td><span class="gp-badge ${c.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${c.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${c.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteCategory(${c.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="5" class="text-center py-4 text-muted">No product categories found</td></tr>';
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

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Category' : 'Add Category';
    if (id) {
        var c = categories.find(function (x) { return x.id === id; });
        document.getElementById('fCategoryName').value = c.categoryName;
        document.getElementById('fDescription').value = c.description || '';
        document.getElementById('fActive').checked = c.isActive;
    } else {
        document.getElementById('fCategoryName').value = '';
        document.getElementById('fDescription').value = '';
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('categoryModal')).show();
}

function saveCategory() {
    var categoryName = document.getElementById('fCategoryName').value.trim();
    if (!categoryName) { toastr.error('Category name is required'); return; }
    var description = document.getElementById('fDescription').value.trim();
    var isActive = document.getElementById('fActive').checked;

    var body = { categoryName: categoryName, description: description || null, isActive: isActive };
    var url = editingId ? '/SupplyChain/UpdateProductCategory?id=' + editingId : '/SupplyChain/CreateProductCategory';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save category'); return; }
        if (editingId) {
            var idx = categories.findIndex(function (c) { return c.id === editingId; });
            if (idx >= 0) categories[idx] = result.data;
            toastr.success('Category updated');
        } else {
            categories.push(result.data);
            toastr.success('Category added');
        }
        bootstrap.Modal.getInstance(document.getElementById('categoryModal')).hide();
        renderTable();
    });
}

function deleteCategory(id) {
    confirmDelete('This product category will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/SupplyChain/DeleteProductCategory?id=' + id, { method: 'POST' })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete category'); return; }
                categories = categories.filter(function (c) { return c.id !== id; });
                deletedAlert('Category deleted.');
                renderTable();
            });
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () {
    renderTable();
});
