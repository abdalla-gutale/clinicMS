var vendors = (typeof VENDORS_DATA !== 'undefined' ? VENDORS_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', editingId = null;

function filtered() {
    var q = searchQuery.toLowerCase();
    return vendors.filter(function (v) {
        return !q || v.vendorName.toLowerCase().includes(q) || (v.contactPerson || '').toLowerCase().includes(q);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('vendorsTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (v, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(v.vendorName)}</span></td>
            <td>${escapeHtml(v.contactPerson || '')}</td>
            <td style="color:#64748b;">${escapeHtml(v.phone || '')}</td>
            <td style="color:#64748b;">${escapeHtml(v.email || '')}</td>
            <td><span class="gp-badge ${v.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${v.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${v.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteVendor(${v.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="7" class="text-center py-4 text-muted">No vendors found</td></tr>';
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
    document.getElementById('modalTitle').textContent = id ? 'Edit Vendor' : 'Add Vendor';
    if (id) {
        var v = vendors.find(function (x) { return x.id === id; });
        document.getElementById('fVendorName').value = v.vendorName;
        document.getElementById('fContactPerson').value = v.contactPerson || '';
        document.getElementById('fPhone').value = v.phone || '';
        document.getElementById('fEmail').value = v.email || '';
        document.getElementById('fActive').checked = v.isActive;
    } else {
        document.getElementById('fVendorName').value = '';
        document.getElementById('fContactPerson').value = '';
        document.getElementById('fPhone').value = '';
        document.getElementById('fEmail').value = '';
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('vendorModal')).show();
}

function saveVendor() {
    var vendorName = document.getElementById('fVendorName').value.trim();
    if (!vendorName) { toastr.error('Vendor name is required'); return; }
    var email = document.getElementById('fEmail').value.trim();
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { toastr.error('Enter a valid email address'); return; }

    var body = {
        vendorName: vendorName,
        contactPerson: document.getElementById('fContactPerson').value.trim() || null,
        phone: document.getElementById('fPhone').value.trim() || null,
        email: email || null,
        isActive: document.getElementById('fActive').checked
    };
    var url = editingId ? '/SupplyChain/UpdateVendor?id=' + editingId : '/SupplyChain/CreateVendor';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save vendor'); return; }
        if (editingId) {
            var idx = vendors.findIndex(function (v) { return v.id === editingId; });
            if (idx >= 0) vendors[idx] = result.data;
            toastr.success('Vendor updated');
        } else {
            vendors.push(result.data);
            toastr.success('Vendor added');
        }
        bootstrap.Modal.getInstance(document.getElementById('vendorModal')).hide();
        renderTable();
    });
}

function deleteVendor(id) {
    confirmDelete('This vendor will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/SupplyChain/DeleteVendor?id=' + id, { method: 'POST' })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete vendor'); return; }
                vendors = vendors.filter(function (v) { return v.id !== id; });
                deletedAlert('Vendor deleted.');
                renderTable();
            });
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });
