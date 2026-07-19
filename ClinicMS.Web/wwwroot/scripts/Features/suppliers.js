var suppliers = (typeof SUPPLIERS_DATA !== 'undefined' ? SUPPLIERS_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', editingId = null;

function filtered() {
    var q = searchQuery.toLowerCase();
    return suppliers.filter(function (s) {
        return !q || s.supplierName.toLowerCase().includes(q) || (s.contactPerson || '').toLowerCase().includes(q);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('suppliersTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (s, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${s.supplierName}</span></td>
            <td>${s.contactPerson || ''}</td>
            <td style="color:#64748b;">${s.phone || ''}</td>
            <td style="color:#64748b;">${s.email || ''}</td>
            <td><span class="gp-badge ${s.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${s.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${s.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteSupplier(${s.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="7" class="text-center py-4 text-muted">No suppliers found</td></tr>';
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
    document.getElementById('modalTitle').textContent = id ? 'Edit Supplier' : 'Add Supplier';
    if (id) {
        var s = suppliers.find(function (x) { return x.id === id; });
        document.getElementById('fSupplierName').value = s.supplierName;
        document.getElementById('fContactPerson').value = s.contactPerson || '';
        document.getElementById('fPhone').value = s.phone || '';
        document.getElementById('fEmail').value = s.email || '';
        document.getElementById('fAddress').value = s.address || '';
        document.getElementById('fActive').checked = s.isActive;
    } else {
        document.getElementById('fSupplierName').value = '';
        document.getElementById('fContactPerson').value = '';
        document.getElementById('fPhone').value = '';
        document.getElementById('fEmail').value = '';
        document.getElementById('fAddress').value = '';
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('supplierModal')).show();
}

function saveSupplier() {
    var supplierName = document.getElementById('fSupplierName').value.trim();
    if (!supplierName) { toastr.error('Supplier name is required'); return; }
    var email = document.getElementById('fEmail').value.trim();
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { toastr.error('Enter a valid email address'); return; }

    var body = {
        supplierName: supplierName,
        contactPerson: document.getElementById('fContactPerson').value.trim() || null,
        phone: document.getElementById('fPhone').value.trim() || null,
        email: email || null,
        address: document.getElementById('fAddress').value.trim() || null,
        isActive: document.getElementById('fActive').checked
    };
    var url = editingId ? '/SupplyChain/UpdateSupplier?id=' + editingId : '/SupplyChain/CreateSupplier';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save supplier'); return; }
        if (editingId) {
            var idx = suppliers.findIndex(function (s) { return s.id === editingId; });
            if (idx >= 0) suppliers[idx] = result.data;
            toastr.success('Supplier updated');
        } else {
            suppliers.push(result.data);
            toastr.success('Supplier added');
        }
        bootstrap.Modal.getInstance(document.getElementById('supplierModal')).hide();
        renderTable();
    });
}

function deleteSupplier(id) {
    if (!confirm('Delete this supplier?')) return;
    fetch('/SupplyChain/DeleteSupplier?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete supplier'); return; }
            suppliers = suppliers.filter(function (s) { return s.id !== id; });
            toastr.success('Deleted');
            renderTable();
        });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });
