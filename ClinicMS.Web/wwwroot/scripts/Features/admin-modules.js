var modules = (typeof MODULES_DATA !== 'undefined' ? MODULES_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', editingId = null;

function filtered() {
    var q = searchQuery.toLowerCase();
    return modules.filter(function (m) { return !q || m.moduleName.toLowerCase().includes(q); })
        .sort(function (a, b) { return a.displayOrder - b.displayOrder; });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('modulesTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (m, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="display:inline-flex;align-items:center;justify-content:center;width:32px;height:32px;border-radius:8px;background:#f0fdfa;color:#0d9488;font-size:1.05rem;"><i class="${escapeHtml(m.moduleIcon)}"></i></span></td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(m.moduleName)}</span></td>
            <td style="color:#64748b;">${m.displayOrder}</td>
            <td><span class="gp-badge ${m.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${m.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${m.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteModule(${m.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="6" class="text-center py-4 text-muted">No modules found</td></tr>';
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

function updateModuleIconPreview() {
    var val = document.getElementById('fModuleIcon').value.trim();
    document.getElementById('fModuleIconPreview').innerHTML = '<i class="' + escapeHtml(val || 'ri-question-line') + '"></i>';
}

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Module' : 'Add Module';
    if (id) {
        var m = modules.find(function (x) { return x.id === id; });
        document.getElementById('fModuleName').value = m.moduleName;
        document.getElementById('fModuleIcon').value = m.moduleIcon;
        document.getElementById('fDisplayOrder').value = m.displayOrder;
        document.getElementById('fActive').checked = m.isActive;
    } else {
        document.getElementById('fModuleName').value = '';
        document.getElementById('fModuleIcon').value = '';
        document.getElementById('fDisplayOrder').value = modules.length ? Math.max.apply(null, modules.map(function (m) { return m.displayOrder; })) + 1 : 1;
        document.getElementById('fActive').checked = true;
    }
    updateModuleIconPreview();
    new bootstrap.Modal(document.getElementById('moduleModal')).show();
}

function saveModule() {
    var moduleName = document.getElementById('fModuleName').value.trim();
    var moduleIcon = document.getElementById('fModuleIcon').value.trim();
    var displayOrder = parseInt(document.getElementById('fDisplayOrder').value, 10);
    var isActive = document.getElementById('fActive').checked;

    if (!moduleName) { toastr.error('Module name is required'); return; }
    if (!moduleIcon) { toastr.error('Module icon is required'); return; }
    if (isNaN(displayOrder) || displayOrder < 1) { toastr.error('Enter a valid display order'); return; }

    var body = { moduleName: moduleName, moduleIcon: moduleIcon, displayOrder: displayOrder, isActive: isActive };
    var url = editingId ? '/Administration/UpdateModule?id=' + editingId : '/Administration/CreateModule';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save module'); return; }
        if (editingId) {
            var idx = modules.findIndex(function (m) { return m.id === editingId; });
            if (idx >= 0) modules[idx] = result.data;
            toastr.success('Module updated');
        } else {
            modules.push(result.data);
            toastr.success('Module added');
        }
        bootstrap.Modal.getInstance(document.getElementById('moduleModal')).hide();
        renderTable();
    });
}

function deleteModule(id) {
    confirmDelete('This module will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/Administration/DeleteModule?id=' + id, { method: 'POST' })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete module'); return; }
                modules = modules.filter(function (m) { return m.id !== id; });
                deletedAlert('Module deleted.');
                renderTable();
            });
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });
