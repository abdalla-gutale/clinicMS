var users = (typeof USERS_DATA !== 'undefined' ? USERS_DATA : []);
var roles = (typeof ROLES_DATA !== 'undefined' ? ROLES_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', roleF = '', statusF = '', editingId = null;

var roleColors = { Admin:'purple', Manager:'blue', Trainer:'teal', Receptionist:'green', Accountant:'orange', Viewer:'gray' };

function roleColor(roleName) { return roleColors[roleName] || 'gray'; }

function filtered() {
    return users.filter(function (u) {
        var q = searchQuery.toLowerCase();
        var match = !q || u.username.toLowerCase().includes(q) || u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q);
        var r = !roleF || u.roleName === roleF;
        var s = !statusF || (u.isActive ? 'Active' : 'Inactive') === statusF;
        return match && r && s;
    });
}

function formatDate(iso) {
    if (!iso) return '';
    var d = new Date(iso);
    return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage-1)*perPage, currentPage*perPage);
    var tbody = document.getElementById('usersTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (u, i) { return `
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(u.username)}</span></td>
            <td>${escapeHtml(u.fullName)}</td>
            <td>${escapeHtml(u.email)}</td>
            <td><span class="gp-badge gp-badge-${roleColor(u.roleName)}">${escapeHtml(u.roleName)}</span></td>
            <td><span class="gp-badge ${u.isActive?'gp-badge-green':'gp-badge-red'}">${u.isActive?'Active':'Inactive'}</span></td>
            <td style="font-size:.78rem;color:#64748b;">${formatDate(u.createdAt)}</td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${u.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteUser(${u.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="8" class="text-center py-4 text-muted">No users found</td></tr>';
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

function populateRoleFilter() {
    var sel = document.getElementById('roleFilter');
    sel.innerHTML = '<option value="">All Roles</option>' +
        roles.map(function (r) { return `<option>${r.roleName}</option>`; }).join('');
}

function populateRoleSelect(selectedId) {
    var sel = document.getElementById('fRole');
    sel.innerHTML = roles.filter(function (r) { return r.isActive; }).map(function (r) {
        return `<option value="${r.id}" ${r.id === selectedId ? 'selected' : ''}>${r.roleName}</option>`;
    }).join('');
}

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit User' : 'Add User';
    document.getElementById('passwordField').style.display = id ? 'none' : '';
    if (id) {
        var u = users.find(function (x) { return x.id === id; });
        document.getElementById('fFullName').value = u.fullName;
        document.getElementById('fUsername').value = u.username;
        document.getElementById('fUsername').disabled = true;
        document.getElementById('fEmail').value = u.email;
        document.getElementById('fPhone').value = u.phoneNumber || '';
        populateRoleSelect(u.roleId);
        document.getElementById('fStatus').checked = u.isActive;
    } else {
        document.getElementById('fFullName').value = '';
        document.getElementById('fUsername').value = '';
        document.getElementById('fUsername').disabled = false;
        document.getElementById('fEmail').value = '';
        document.getElementById('fPhone').value = '';
        document.getElementById('fPassword').value = '';
        populateRoleSelect(roles.length ? roles[0].id : null);
        document.getElementById('fStatus').checked = true;
    }
    new bootstrap.Modal(document.getElementById('userModal')).show();
}

function saveUser() {
    var fullName = document.getElementById('fFullName').value.trim();
    var username = document.getElementById('fUsername').value.trim();
    var email    = document.getElementById('fEmail').value.trim();
    var phone    = document.getElementById('fPhone').value.trim();
    var roleId   = parseInt(document.getElementById('fRole').value, 10);
    var isActive = document.getElementById('fStatus').checked;

    if (!fullName || !username || !email) { toastr.error('Full name, username and email are required'); return; }

    if (editingId) {
        fetch('/Users/Update?id=' + editingId, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ roleId: roleId, fullName: fullName, email: email, phoneNumber: phone || null, isActive: isActive })
        }).then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        }).then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not update user'); return; }
            var idx = users.findIndex(function (u) { return u.id === editingId; });
            if (idx >= 0) users[idx] = result.data;
            toastr.success('User updated successfully');
            bootstrap.Modal.getInstance(document.getElementById('userModal')).hide();
            renderTable();
        });
    } else {
        var password = document.getElementById('fPassword').value;
        if (!password) { toastr.error('Password is required'); return; }
        fetch('/Users/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ roleId: roleId, username: username, password: password, fullName: fullName, email: email, phoneNumber: phone || null })
        }).then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        }).then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not create user'); return; }
            users.push(result.data);
            toastr.success('User added successfully');
            bootstrap.Modal.getInstance(document.getElementById('userModal')).hide();
            renderTable();
        });
    }
}

function deleteUser(id) {
    confirmDelete('This user account will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/Users/Delete?id=' + id, { method: 'POST' })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete user'); return; }
                users = users.filter(function (u) { return u.id !== id; });
                deletedAlert('User deleted.');
                renderTable();
            });
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() {
    roleF = document.getElementById('roleFilter').value;
    statusF = document.getElementById('statusFilter').value;
    currentPage = 1; renderTable();
}

document.addEventListener('DOMContentLoaded', function () {
    populateRoleFilter();
    renderTable();
});

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
