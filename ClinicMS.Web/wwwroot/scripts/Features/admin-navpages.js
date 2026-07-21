var navPages = (typeof NAV_PAGES_DATA !== 'undefined' ? NAV_PAGES_DATA : []);
var navModules = (typeof ADMIN_MODULES_DATA !== 'undefined' ? ADMIN_MODULES_DATA : []);

var currentPage = 1, perPage = 10, searchQuery = '', moduleF = '', editingId = null;

function moduleName(id) {
    var m = navModules.find(function (x) { return x.id === id; });
    return m ? m.moduleName : '—';
}
function pageName(id) {
    var p = navPages.find(function (x) { return x.id === id; });
    return p ? p.pageName : '';
}

function filtered() {
    var q = searchQuery.toLowerCase();
    return navPages.filter(function (p) {
        var match = !q || p.pageName.toLowerCase().includes(q) || p.pageUrl.toLowerCase().includes(q);
        var m = !moduleF || p.moduleId === parseInt(moduleF, 10);
        return match && m;
    }).sort(function (a, b) { return a.moduleId - b.moduleId || a.displayOrder - b.displayOrder; });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('navPagesTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (p, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(p.pageName)}</span></td>
            <td><span class="gp-badge gp-badge-teal">${escapeHtml(moduleName(p.moduleId))}</span></td>
            <td style="color:#64748b;">${p.parentPageId ? escapeHtml(pageName(p.parentPageId)) : '—'}</td>
            <td style="color:#64748b;">${escapeHtml(p.pageUrl)}</td>
            <td style="color:#64748b;">${p.displayOrder}</td>
            <td><span class="gp-badge ${p.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${p.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${p.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteNavPage(${p.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="8" class="text-center py-4 text-muted">No nav pages found</td></tr>';
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

function populateModuleFilter() {
    var sel = document.getElementById('moduleFilter');
    sel.innerHTML = '<option value="">All Modules</option>' +
        navModules.map(function (m) { return `<option value="${m.id}">${escapeHtml(m.moduleName)}</option>`; }).join('');
}

function populateModuleSelect(selectedId) {
    var sel = document.getElementById('fModule');
    sel.innerHTML = navModules.map(function (m) {
        return `<option value="${m.id}" ${m.id === selectedId ? 'selected' : ''}>${escapeHtml(m.moduleName)}</option>`;
    }).join('');
}

function populateParentSelect(moduleId, selectedId) {
    var sel = document.getElementById('fParentPage');
    var candidates = navPages.filter(function (p) {
        return p.moduleId === moduleId && !p.parentPageId && p.id !== editingId;
    });
    sel.innerHTML = '<option value="">None (top-level)</option>' + candidates.map(function (p) {
        return `<option value="${p.id}" ${p.id === selectedId ? 'selected' : ''}>${escapeHtml(p.pageName)}</option>`;
    }).join('');
}

function onModuleChange() {
    var moduleId = parseInt(document.getElementById('fModule').value, 10);
    populateParentSelect(moduleId, null);
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) { $('#fParentPage').trigger('change'); }
}

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Nav Page' : 'Add Nav Page';
    if (id) {
        var p = navPages.find(function (x) { return x.id === id; });
        document.getElementById('fPageName').value = p.pageName;
        populateModuleSelect(p.moduleId);
        populateParentSelect(p.moduleId, p.parentPageId);
        document.getElementById('fPageUrl').value = p.pageUrl;
        document.getElementById('fDisplayOrder').value = p.displayOrder;
        document.getElementById('fActive').checked = p.isActive;
    } else {
        document.getElementById('fPageName').value = '';
        populateModuleSelect(navModules.length ? navModules[0].id : null);
        populateParentSelect(navModules.length ? navModules[0].id : null, null);
        document.getElementById('fPageUrl').value = '';
        document.getElementById('fDisplayOrder').value = 1;
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('navPageModal')).show();
}

function saveNavPage() {
    var pageName = document.getElementById('fPageName').value.trim();
    var moduleId = parseInt(document.getElementById('fModule').value, 10);
    var parentRaw = document.getElementById('fParentPage').value;
    var parentPageId = parentRaw ? parseInt(parentRaw, 10) : null;
    var pageUrl = document.getElementById('fPageUrl').value.trim();
    var displayOrder = parseInt(document.getElementById('fDisplayOrder').value, 10);
    var isActive = document.getElementById('fActive').checked;

    if (!pageName) { toastr.error('Page name is required'); return; }
    if (!moduleId) { toastr.error('Module is required'); return; }
    if (!pageUrl || pageUrl[0] !== '/') { toastr.error('Page URL must start with /'); return; }
    if (isNaN(displayOrder) || displayOrder < 1) { toastr.error('Enter a valid display order'); return; }

    var body = { moduleId: moduleId, pageName: pageName, pageUrl: pageUrl, displayOrder: displayOrder, isActive: isActive, parentPageId: parentPageId };
    var url = editingId ? '/Administration/UpdateNavPage?id=' + editingId : '/Administration/CreateNavPage';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save nav page'); return; }
        if (editingId) {
            var idx = navPages.findIndex(function (p) { return p.id === editingId; });
            if (idx >= 0) navPages[idx] = result.data;
            toastr.success('Nav page updated');
        } else {
            navPages.push(result.data);
            toastr.success('Nav page added');
        }
        bootstrap.Modal.getInstance(document.getElementById('navPageModal')).hide();
        renderTable();
    });
}

function deleteNavPage(id) {
    confirmDelete('This nav page will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/Administration/DeleteNavPage?id=' + id, { method: 'POST' })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete nav page'); return; }
                navPages = navPages.filter(function (p) { return p.id !== id; });
                deletedAlert('Nav page deleted.');
                renderTable();
            });
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() { moduleF = document.getElementById('moduleFilter').value; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () {
    populateModuleFilter();
    renderTable();
});

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
