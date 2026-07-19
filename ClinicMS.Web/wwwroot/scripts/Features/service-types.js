var serviceTypes = (typeof SERVICE_TYPES_DATA !== 'undefined' ? SERVICE_TYPES_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', editingId = null;

function filtered() {
    var q = searchQuery.toLowerCase();
    return serviceTypes.filter(function (t) {
        return !q || t.typeName.toLowerCase().includes(q) || (t.description || '').toLowerCase().includes(q);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('typesTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (t, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${t.typeName}</span></td>
            <td style="color:#64748b;">${t.description || ''}</td>
            <td><span class="gp-badge ${t.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${t.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${t.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteType(${t.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="5" class="text-center py-4 text-muted">No service types found</td></tr>';
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
    document.getElementById('modalTitle').textContent = id ? 'Edit Service Type' : 'Add Service Type';
    if (id) {
        var t = serviceTypes.find(function (x) { return x.id === id; });
        document.getElementById('fTypeName').value = t.typeName;
        document.getElementById('fDescription').value = t.description || '';
        document.getElementById('fActive').checked = t.isActive;
    } else {
        document.getElementById('fTypeName').value = '';
        document.getElementById('fDescription').value = '';
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('typeModal')).show();
}

function saveType() {
    var typeName = document.getElementById('fTypeName').value.trim();
    if (!typeName) { toastr.error('Type name is required'); return; }
    var description = document.getElementById('fDescription').value.trim();
    var isActive = document.getElementById('fActive').checked;

    var body = { typeName: typeName, description: description || null, isActive: isActive };
    var url = editingId ? '/MedicalServices/UpdateServiceType?id=' + editingId : '/MedicalServices/CreateServiceType';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save service type'); return; }
        if (editingId) {
            var idx = serviceTypes.findIndex(function (t) { return t.id === editingId; });
            if (idx >= 0) serviceTypes[idx] = result.data;
            toastr.success('Service type updated');
        } else {
            serviceTypes.push(result.data);
            toastr.success('Service type added');
        }
        bootstrap.Modal.getInstance(document.getElementById('typeModal')).hide();
        renderTable();
    });
}

function deleteType(id) {
    if (!confirm('Delete this service type?')) return;
    fetch('/MedicalServices/DeleteServiceType?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete service type'); return; }
            serviceTypes = serviceTypes.filter(function (t) { return t.id !== id; });
            toastr.success('Deleted');
            renderTable();
        });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () {
    renderTable();
});
