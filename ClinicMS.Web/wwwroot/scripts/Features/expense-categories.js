var categories = (typeof CATEGORIES_DATA !== 'undefined' ? CATEGORIES_DATA : []);
var categoryTotals = (typeof CATEGORY_TOTALS !== 'undefined' ? CATEGORY_TOTALS : {});

var currentPage = 1, perPage = 10, searchQuery = '', editingId = null;

function filtered() {
    var q = searchQuery.toLowerCase();
    return categories.filter(function (c) {
        return !q || c.categoryName.toLowerCase().includes(q) || (c.description || '').toLowerCase().includes(q);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total/perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage-1)*perPage, currentPage*perPage);
    document.getElementById('catsTableBody').innerHTML = slice.length ? slice.map(function (c, i) { return `
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${c.categoryName}</span></td>
            <td style="color:#64748b;">${c.description || ''}</td>
            <td><span style="font-weight:700;color:#dc2626;">${(categoryTotals[c.id] || 0).toLocaleString()}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${c.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteCategory(${c.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="5" class="text-center py-4 text-muted">No categories found</td></tr>';
    document.getElementById('pageInfo').textContent = `Showing ${slice.length} of ${total}`;
    var btns = document.getElementById('pageBtns');
    btns.innerHTML = '';
    for (var p = 1; p <= pages; p++) {
        var btn = document.createElement('button');
        btn.className = 'gp-page-btn' + (p===currentPage?' active':'');
        btn.textContent = p;
        btn.onclick = (function(pp){ return function(){ currentPage=pp; renderTable(); }; })(p);
        btns.appendChild(btn);
    }
}

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Category' : 'Add Category';
    if (id) {
        var c = categories.find(function (x) { return x.id === id; });
        document.getElementById('fName').value = c.categoryName;
        document.getElementById('fDesc').value = c.description || '';
    } else {
        document.getElementById('fName').value = '';
        document.getElementById('fDesc').value = '';
    }
    new bootstrap.Modal(document.getElementById('catModal')).show();
}

function saveCategory() {
    var name = document.getElementById('fName').value.trim();
    if (!name) { toastr.error('Name required'); return; }
    var desc = document.getElementById('fDesc').value.trim();

    if (editingId) {
        fetch('/Expenses/UpdateCategory?id=' + editingId, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ categoryName: name, description: desc || null, isActive: true })
        }).then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        }).then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not update category'); return; }
            var idx = categories.findIndex(function (c) { return c.id === editingId; });
            if (idx >= 0) categories[idx] = result.data;
            toastr.success('Updated');
            bootstrap.Modal.getInstance(document.getElementById('catModal')).hide();
            renderTable();
        });
    } else {
        fetch('/Expenses/CreateCategory', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ categoryName: name, description: desc || null, isActive: true })
        }).then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        }).then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not add category'); return; }
            categories.push(result.data);
            toastr.success('Added');
            bootstrap.Modal.getInstance(document.getElementById('catModal')).hide();
            renderTable();
        });
    }
}

function deleteCategory(id) {
    if (!confirm('Delete?')) return;
    fetch('/Expenses/DeleteCategory?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete category'); return; }
            categories = categories.filter(function (c) { return c.id !== id; });
            toastr.success('Deleted');
            renderTable();
        });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
document.addEventListener('DOMContentLoaded', renderTable);

// -- Select2 init --
function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({
            theme: 'default',
            width: '100%',
            dropdownParent: document.body
        });
    }
}
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
