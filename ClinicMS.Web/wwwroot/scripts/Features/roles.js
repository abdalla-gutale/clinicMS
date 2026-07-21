var roles    = (typeof ROLES_DATA    !== 'undefined' ? ROLES_DATA    : []);
var modules  = (typeof MODULES_DATA  !== 'undefined' ? MODULES_DATA  : []);
var navPages = (typeof NAVPAGES_DATA !== 'undefined' ? NAVPAGES_DATA : []);

var currentPage = 1, perPage = 10, searchQuery = '', editingId = null, permRoleId = null;
var roleColors = { Admin:'purple', Manager:'blue', Trainer:'teal', Receptionist:'green', Accountant:'orange', Viewer:'gray' };

// ── Table ──────────────────────────────────────────────────
function filtered() {
    var q = searchQuery.toLowerCase();
    return roles.filter(function (r) {
        return !q || r.roleName.toLowerCase().includes(q) || (r.description || '').toLowerCase().includes(q);
    });
}

function renderTable() {
    var data  = filtered();
    var total = data.length;
    var pages = Math.ceil(total/perPage)||1;
    if (currentPage>pages) currentPage=1;
    var slice = data.slice((currentPage-1)*perPage, currentPage*perPage);
    document.getElementById('rolesTableBody').innerHTML = slice.length ? slice.map(function (r, i) { return `
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(r.roleName)}</span></td>
            <td style="color:#64748b;">${escapeHtml(r.description || '')}</td>
            <td><span class="gp-badge ${r.isActive?'gp-badge-green':'gp-badge-red'}">${r.isActive?'Active':'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" title="Edit" onclick="openModal(${r.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-perm" title="Permissions" onclick="openPermModal(${r.id})"><i class="ri-shield-keyhole-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" title="Delete" onclick="deleteRole(${r.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="5" class="text-center py-4 text-muted">No roles found</td></tr>';

    document.getElementById('pageInfo').textContent = `Showing ${slice.length} of ${total}`;
    var btns = document.getElementById('pageBtns');
    btns.innerHTML = '';
    for (var p=1;p<=pages;p++) {
        var btn = document.createElement('button');
        btn.className = 'gp-page-btn'+(p===currentPage?' active':'');
        btn.textContent = p;
        btn.onclick = (function(pp){ return function(){ currentPage=pp; renderTable(); }; })(p);
        btns.appendChild(btn);
    }
}

// ── Add/Edit Role Modal ────────────────────────────────────
function openModal(id) {
    editingId = id||null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Role' : 'Add Role';
    if (id) {
        var r = roles.find(function (x) { return x.id === id; });
        document.getElementById('fName').value   = r.roleName;
        document.getElementById('fDesc').value   = r.description || '';
        document.getElementById('fStatus').checked = r.isActive;
    } else {
        document.getElementById('fName').value   = '';
        document.getElementById('fDesc').value   = '';
        document.getElementById('fStatus').checked = true;
    }
    new bootstrap.Modal(document.getElementById('roleModal')).show();
}

function saveRole() {
    var name = document.getElementById('fName').value.trim();
    if (!name) { toastr.error('Role name is required'); return; }
    var desc = document.getElementById('fDesc').value.trim();
    var isActive = document.getElementById('fStatus').checked;

    if (editingId) {
        fetch('/Roles/Update?id=' + editingId, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ roleName: name, description: desc || null, isActive: isActive })
        }).then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        }).then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not update role'); return; }
            var idx = roles.findIndex(function (r) { return r.id === editingId; });
            if (idx >= 0) roles[idx] = result.data;
            toastr.success('Role updated');
            bootstrap.Modal.getInstance(document.getElementById('roleModal')).hide();
            renderTable();
        });
    } else {
        fetch('/Roles/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ roleName: name, description: desc || null, isActive: isActive })
        }).then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        }).then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not create role'); return; }
            roles.push(result.data);
            toastr.success('Role added');
            bootstrap.Modal.getInstance(document.getElementById('roleModal')).hide();
            renderTable();
        });
    }
}

function deleteRole(id) {
    confirmDelete('This role will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/Roles/Delete?id=' + id, { method: 'POST' })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete role'); return; }
                roles = roles.filter(function (r) { return r.id !== id; });
                deletedAlert('Role deleted.');
                renderTable();
            });
    });
}

// ── Permissions Modal ──────────────────────────────────────
// Real backend model: Module -> NavPage -> {CanView, CanCreate, CanEdit, CanDelete}. No fabricated
// per-module action taxonomy -- every nav page gets the same 4 CRUD toggles.
var ACTIONS = [
    { key: 'canView',   label: 'View',   icon: 'ri-eye-line',         cls: 'i-view'   },
    { key: 'canCreate', label: 'Create', icon: 'ri-add-circle-line',  cls: 'i-create' },
    { key: 'canEdit',   label: 'Edit',   icon: 'ri-pencil-line',       cls: 'i-edit'   },
    { key: 'canDelete', label: 'Delete', icon: 'ri-delete-bin-line',   cls: 'i-delete' }
];

function openPermModal(id) {
    permRoleId = id;
    var role = roles.find(function (r) { return r.id === id; });
    document.getElementById('permModalTitle').textContent = 'Manage Permissions – ' + role.roleName;
    document.getElementById('permModalBody').innerHTML = '<div class="text-center text-muted py-4">Loading…</div>';
    new bootstrap.Modal(document.getElementById('permModal')).show();

    fetch('/Roles/Permissions?id=' + id)
        .then(function (res) { return res.json(); })
        .then(function (roleWithPermissions) {
            var granted = {};
            (roleWithPermissions.navPermissions || []).forEach(function (p) { granted[p.navPageId] = p; });
            renderPermModal(granted);
        });
}

function renderPermModal(granted) {
    var html = '';
    modules.forEach(function (mod) {
        var pages = navPages.filter(function (p) { return p.moduleId === mod.id; });
        if (!pages.length) return;

        var modEnabled = pages.some(function (p) { return granted[p.id] && ACTIONS.some(function (a) { return granted[p.id][a.key]; }); });

        html += `
        <div class="pm-module ${modEnabled ? 'open' : ''}" id="mod-${mod.id}">
            <div class="pm-module-header" onclick="toggleModule(${mod.id})">
                <div class="pm-module-chevron"><i class="ri-arrow-right-s-line"></i></div>
                <span class="pm-module-name">${escapeHtml(mod.moduleName)}</span>
                <label class="pm-toggle mod-toggle" onclick="event.stopPropagation()" title="Enable module">
                    <input type="checkbox" id="modtog-${mod.id}" ${modEnabled ? 'checked' : ''}
                           onchange="toggleModuleAll(${mod.id}, this.checked)">
                    <span class="pm-slider"></span>
                </label>
            </div>
            <div class="pm-module-body">`;

        pages.forEach(function (page) {
            var g = granted[page.id] || {};
            var allChecked = ACTIONS.every(function (a) { return !!g[a.key]; });

            html += `
                <div class="pm-sub" data-page-id="${page.id}">
                    <div class="pm-sub-header">
                        <span class="pm-sub-name">${escapeHtml(page.pageName)}</span>
                        <label class="pm-select-all-label">
                            <label class="pm-toggle" onclick="event.stopPropagation()">
                                <input type="checkbox" class="sub-sel-all" data-page="${page.id}"
                                       ${allChecked ? 'checked' : ''}
                                       onchange="toggleSubAll(this)">
                                <span class="pm-slider"></span>
                            </label>
                            <span>Select All</span>
                        </label>
                    </div>
                    <div class="pm-actions">`;

            ACTIONS.forEach(function (act) {
                html += `
                        <div class="pm-action-item">
                            <i class="pm-action-icon ${act.cls} ${act.icon}"></i>
                            <span class="pm-action-name">${act.label}</span>
                            <label class="pm-toggle">
                                <input type="checkbox" class="perm-cb" data-page="${page.id}" data-action="${act.key}"
                                       data-mod="${mod.id}"
                                       ${g[act.key] ? 'checked' : ''}
                                       onchange="onPermChange(this)">
                                <span class="pm-slider"></span>
                            </label>
                        </div>`;
            });

            html += `
                    </div>
                </div>`;
        });

        html += `
            </div>
        </div>`;
    });

    document.getElementById('permModalBody').innerHTML = html || '<div class="text-center text-muted py-4">No modules configured.</div>';
}

function toggleModule(modId) {
    var el = document.getElementById('mod-' + modId);
    el.classList.toggle('open');
}

function toggleModuleAll(modId, checked) {
    var el = document.getElementById('mod-' + modId);
    el.querySelectorAll('.perm-cb').forEach(function (cb) { cb.checked = checked; });
    el.querySelectorAll('.sub-sel-all').forEach(function (cb) { cb.checked = checked; });
    if (checked) el.classList.add('open');
}

function toggleSubAll(selAllCb) {
    var pageId = selAllCb.dataset.page;
    document.querySelectorAll('.perm-cb[data-page="' + pageId + '"]').forEach(function (cb) {
        cb.checked = selAllCb.checked;
    });
    syncModuleToggle(selAllCb);
}

function onPermChange(cb) {
    var pageId = cb.dataset.page;
    var pageBoxes = document.querySelectorAll('.perm-cb[data-page="' + pageId + '"]');
    var allChecked = Array.from(pageBoxes).every(function (c) { return c.checked; });
    var selAll = cb.closest('.pm-sub').querySelector('.sub-sel-all');
    if (selAll) selAll.checked = allChecked;
    syncModuleToggle(cb);
}

function syncModuleToggle(el) {
    var modEl = el.closest('.pm-module');
    if (!modEl) return;
    var anyChecked = Array.from(modEl.querySelectorAll('.perm-cb')).some(function (c) { return c.checked; });
    var modToggle = modEl.querySelector('.mod-toggle input');
    if (modToggle) modToggle.checked = anyChecked;
}

function savePermissions() {
    if (!permRoleId) return;
    var permissions = Array.from(document.querySelectorAll('#permModalBody .pm-sub')).map(function (subEl) {
        var pageId = parseInt(subEl.dataset.pageId, 10);
        var get = function (action) {
            var cb = subEl.querySelector('.perm-cb[data-action="' + action + '"]');
            return !!(cb && cb.checked);
        };
        return { navPageId: pageId, canView: get('canView'), canCreate: get('canCreate'), canEdit: get('canEdit'), canDelete: get('canDelete') };
    });

    fetch('/Roles/SavePermissions?id=' + permRoleId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(permissions)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save permissions'); return; }
        toastr.success('Permissions saved');
        bootstrap.Modal.getInstance(document.getElementById('permModal')).hide();
    });
}

// ── Helpers ────────────────────────────────────────────────
function handleSearch(v) { searchQuery=v; currentPage=1; renderTable(); }

document.addEventListener('DOMContentLoaded', renderTable);

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme:'default', width:'100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
