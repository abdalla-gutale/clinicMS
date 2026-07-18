// ── Mock session user ─────────────────────────────────────
var PROFILE_USER = (function () {
    try {
        var stored = sessionStorage.getItem('authUser') || sessionStorage.getItem('pendingUser');
        if (stored) return JSON.parse(stored);
    } catch (e) {}
    return { name: 'Ahmed Hassan', email: 'ahmed.h@gym.com', branch: 'Main Branch', role: 'Admin' };
})();

// ── Mock activity log for this user ──────────────────────
var MY_ACTIVITY = [
    { action: 'LOGIN',  icon: 'ri-login-circle-line', cls: 'c-blue',  msg: 'Signed in successfully',                   time: 'Today, 09:15' },
    { action: 'UPDATE', icon: 'ri-edit-line',          cls: 'c-amber', msg: 'Updated member profile – Mahmoud Samir',   time: 'Today, 09:22' },
    { action: 'CREATE', icon: 'ri-add-circle-line',    cls: 'c-green', msg: 'Added new member – Layla Ibrahim',         time: 'Today, 09:45' },
    { action: 'UPDATE', icon: 'ri-edit-line',          cls: 'c-amber', msg: 'Modified shift schedule – Morning Shift',  time: 'Today, 10:10' },
    { action: 'DELETE', icon: 'ri-delete-bin-line',    cls: 'c-red',   msg: 'Removed expired subscription #1042',       time: 'Today, 10:30' },
    { action: 'CREATE', icon: 'ri-add-circle-line',    cls: 'c-green', msg: 'Created payment record – INV-20548',       time: 'Today, 11:00' },
    { action: 'UPDATE', icon: 'ri-settings-3-line',    cls: 'c-teal',  msg: 'Changed general settings – SMS provider',  time: 'Today, 11:20' },
    { action: 'CREATE', icon: 'ri-add-circle-line',    cls: 'c-green', msg: 'Registered new employee – Karim Adel',     time: 'Yesterday, 16:05' },
    { action: 'UPDATE', icon: 'ri-edit-line',          cls: 'c-amber', msg: 'Updated package price – Pro Monthly',      time: 'Yesterday, 16:30' },
    { action: 'LOGIN',  icon: 'ri-login-circle-line',  cls: 'c-blue',  msg: 'Signed in successfully',                   time: 'Yesterday, 08:55' },
    { action: 'DELETE', icon: 'ri-delete-bin-line',    cls: 'c-red',   msg: 'Deleted draft invoice #2031',              time: 'Yesterday, 09:10' },
    { action: 'CREATE', icon: 'ri-add-circle-line',    cls: 'c-green', msg: 'Added equipment item – Treadmill #8',      time: '2 days ago, 14:00' },
    { action: 'UPDATE', icon: 'ri-edit-line',          cls: 'c-amber', msg: 'Edited employee record – Sara Mohamed',    time: '2 days ago, 14:45' },
    { action: 'CREATE', icon: 'ri-add-circle-line',    cls: 'c-green', msg: 'Created expense – Electricity Bill',       time: '3 days ago, 10:00' },
    { action: 'LOGIN',  icon: 'ri-login-circle-line',  cls: 'c-blue',  msg: 'Signed in successfully',                   time: '3 days ago, 08:30' },
    { action: 'UPDATE', icon: 'ri-edit-line',          cls: 'c-amber', msg: 'Updated notification rules',               time: '4 days ago, 15:20' },
    { action: 'CREATE', icon: 'ri-add-circle-line',    cls: 'c-green', msg: 'Added new subscription – Rana Youssef',    time: '4 days ago, 11:05' },
    { action: 'DELETE', icon: 'ri-delete-bin-line',    cls: 'c-red',   msg: 'Removed old equipment – Bike #3',          time: '5 days ago, 13:00' },
    { action: 'UPDATE', icon: 'ri-edit-line',          cls: 'c-amber', msg: 'Changed branch info – North Branch',       time: '6 days ago, 09:45' },
    { action: 'LOGIN',  icon: 'ri-login-circle-line',  cls: 'c-blue',  msg: 'Signed in successfully',                   time: '7 days ago, 08:00' },
];

var LOGIN_HISTORY = [
    { device: 'Chrome on Windows',   ip: '192.168.1.10', time: 'Today, 09:15',       status: 'current' },
    { device: 'Chrome on Windows',   ip: '192.168.1.10', time: 'Yesterday, 08:55',   status: 'ok' },
    { device: 'Firefox on Windows',  ip: '192.168.1.12', time: '3 days ago, 08:30',  status: 'ok' },
    { device: 'Chrome on Windows',   ip: '192.168.1.10', time: '7 days ago, 08:00',  status: 'ok' },
];

var MY_PERMISSIONS = [
    'Users: View', 'Users: Create', 'Users: Edit', 'Users: Delete',
    'Employees: View', 'Employees: Create', 'Employees: Edit', 'Employees: Delete',
    'Members: View', 'Members: Create', 'Members: Edit', 'Members: Delete',
    'Packages: View', 'Packages: Create', 'Packages: Edit',
    'Payments: View', 'Payments: Record', 'Invoices: View', 'Invoices: Print',
    'Expenses: View', 'Settings: View', 'Settings: Edit',
];

var ACTIVITY_PAGE = 1;
var ACTIVITY_PER_PAGE = 10;

// ── Init ──────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    fillUserInfo();
    renderActivityFeed(1);
    renderLoginHistory();
    renderPermissions();
    renderActionChart();
    fillEditForm();
});

function initials(name) {
    return name.split(' ').map(function (w) { return w[0]; }).join('').toUpperCase().slice(0, 2);
}

function roleColor(role) {
    var m = { Admin: 'gp-badge-purple', Manager: 'gp-badge-blue', Trainer: 'gp-badge-teal', Receptionist: 'gp-badge-green', Accountant: 'gp-badge-orange' };
    return m[role] || 'gp-badge-gray';
}

function fillUserInfo() {
    var u = PROFILE_USER;
    var init = initials(u.name);

    // Hero
    setText('heroName', u.name);
    setText('heroMeta', u.role + ' · ' + u.branch);

    // Avatar row
    setText('profAvatar', init);
    setText('profDisplayName', u.name);
    setText('profBranch', u.branch);
    var badge = document.getElementById('profRoleBadge');
    if (badge) { badge.textContent = u.role; badge.className = 'gp-badge ' + roleColor(u.role); }

    // Info tab
    setText('infoName', u.name);
    setText('infoEmail', u.email || 'ahmed.h@gym.com');
    setText('infoRole', u.role);
    setText('infoBranch', u.branch);
    setText('infoUsername', u.name.toLowerCase().replace(' ', '.'));
}

function fillEditForm() {
    var u = PROFILE_USER;
    setVal('editName',   u.name);
    setVal('editEmail',  u.email || 'ahmed.h@gym.com');
    setVal('editRole',   u.role);
    setVal('editBranch', u.branch);
}

function renderActivityFeed(page) {
    ACTIVITY_PAGE = page || 1;
    var feed = document.getElementById('myActivityFeed');
    if (!feed) return;
    var total = MY_ACTIVITY.length;
    var totalPages = Math.ceil(total / ACTIVITY_PER_PAGE);
    var start = (ACTIVITY_PAGE - 1) * ACTIVITY_PER_PAGE;
    var slice = MY_ACTIVITY.slice(start, start + ACTIVITY_PER_PAGE);

    feed.innerHTML = slice.map(function (a) {
        return '<div class="prof-activity-item">' +
            '<div class="prof-act-dot ' + a.cls + '"><i class="' + a.icon + '"></i></div>' +
            '<div class="prof-act-body">' +
                '<div class="prof-act-msg">' +
                    '<span style="display:inline-flex;align-items:center;padding:2px 8px;border-radius:6px;font-size:.65rem;font-weight:800;letter-spacing:.06em;text-transform:uppercase;margin-right:6px;background:' + actionBg(a.action) + ';color:' + actionColor(a.action) + '">' + a.action + '</span>' +
                    a.msg +
                '</div>' +
                '<div class="prof-act-time"><i class="ri-time-line" style="font-size:.75rem;"></i> ' + a.time + '</div>' +
            '</div>' +
        '</div>';
    }).join('');

    // Pagination
    var pgWrap = document.getElementById('activityPagination');
    if (!pgWrap) return;
    var from = start + 1, to = Math.min(start + ACTIVITY_PER_PAGE, total);
    pgWrap.style.display = totalPages <= 1 ? 'none' : '';

    function pgBtn(p, label, disabled, active) {
        var cls = 'al-page-btn' + (active ? ' active' : '');
        var attrs = disabled ? ' disabled style="opacity:.4;cursor:default;pointer-events:none;"' : '';
        return '<button onclick="renderActivityFeed(' + p + ')" class="' + cls + '"' + attrs + '>' + label + '</button>';
    }
    function ellipsis() {
        return '<button class="al-page-btn" style="cursor:default;pointer-events:none;border-color:transparent;background:transparent;">…</button>';
    }

    var p = ACTIVITY_PAGE;
    var btns = pgBtn(p - 1, '<i class="ri-arrow-left-s-line"></i>', p === 1, false);
    // Always show page 1
    btns += pgBtn(1, '1', false, p === 1);
    // Always show page 2 if exists
    if (totalPages >= 2) btns += pgBtn(2, '2', false, p === 2);
    // Ellipsis before current if far right
    if (p > 3) btns += ellipsis();
    // Current page if not 1, 2 or last
    if (p > 2 && p < totalPages) btns += pgBtn(p, p, false, true);
    // Ellipsis before last if needed
    if (p < totalPages - 1 && totalPages > 3) btns += ellipsis();
    // Always show last page if > 2
    if (totalPages > 2) btns += pgBtn(totalPages, totalPages, false, p === totalPages);
    btns += pgBtn(p + 1, '<i class="ri-arrow-right-s-line"></i>', p === totalPages, false);

    pgWrap.innerHTML =
        '<span class="al-page-info">Showing ' + from + '–' + to + ' of ' + total + '</span>' +
        '<div class="al-page-btns">' + btns + '</div>';
}

function actionBg(a)    { return {LOGIN:'#dbeafe',CREATE:'#dcfce7',UPDATE:'#fef9c3',DELETE:'#fee2e2'}[a] || '#f1f5f9'; }
function actionColor(a) { return {LOGIN:'#1d4ed8',CREATE:'#15803d',UPDATE:'#a16207',DELETE:'#b91c1c'}[a] || '#475569'; }

function renderLoginHistory() {
    var el = document.getElementById('loginHistory');
    if (!el) return;
    el.innerHTML = LOGIN_HISTORY.map(function (l) {
        var isCurrent = l.status === 'current';
        return '<div class="prof-activity-item">' +
            '<div class="prof-act-dot ' + (isCurrent ? 'c-green' : 'c-blue') + '"><i class="ri-device-line"></i></div>' +
            '<div class="prof-act-body">' +
                '<div class="prof-act-msg">' + l.device + (isCurrent ? ' <span class="gp-badge gp-badge-green" style="font-size:.65rem;">Current</span>' : '') + '</div>' +
                '<div class="prof-act-time">' + l.ip + ' · ' + l.time + '</div>' +
            '</div>' +
        '</div>';
    }).join('');
}

function renderPermissions() {
    var el = document.getElementById('permsGrid');
    if (!el) return;
    var groups = {};
    MY_PERMISSIONS.forEach(function (p) {
        var parts = p.split(': ');
        var mod = parts[0]; var act = parts[1];
        if (!groups[mod]) groups[mod] = [];
        groups[mod].push(act);
    });
    var html = '';
    Object.keys(groups).forEach(function (mod) {
        html += '<div style="margin-bottom:16px;">';
        html += '<div style="font-size:.72rem;font-weight:800;color:#94a3b8;text-transform:uppercase;letter-spacing:.08em;margin-bottom:8px;">' + mod + '</div>';
        html += '<div>';
        groups[mod].forEach(function (act) {
            html += '<span class="perm-chip"><i class="ri-check-line"></i>' + act + '</span>';
        });
        html += '</div></div>';
    });
    el.innerHTML = html;
    var cnt = document.getElementById('totalPermsCount');
    if (cnt) cnt.textContent = MY_PERMISSIONS.length + ' permissions';
}

function renderActionChart() {
    var ctx = document.getElementById('myActionChart');
    if (!ctx) return;
    var counts = { LOGIN: 0, CREATE: 0, UPDATE: 0, DELETE: 0 };
    MY_ACTIVITY.forEach(function (a) { if (counts[a.action] !== undefined) counts[a.action]++; });
    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Login', 'Create', 'Update', 'Delete'],
            datasets: [{ data: [counts.LOGIN, counts.CREATE, counts.UPDATE, counts.DELETE], backgroundColor: ['#3b82f6','#16a34a','#d97706','#dc2626'], borderWidth: 0, hoverOffset: 6 }]
        },
        options: {
            cutout: '65%',
            plugins: { legend: { position: 'bottom', labels: { font: { size: 11, family: 'Poppins' }, usePointStyle: true } } }
        }
    });
}

// ── Tab switching ─────────────────────────────────────────
function switchTab(name) {
    document.querySelectorAll('.prof-tab').forEach(function (t) { t.classList.remove('active'); });
    document.querySelectorAll('.prof-panel').forEach(function (p) { p.classList.remove('active'); });
    var tab = document.getElementById('tab-' + name);
    var panel = document.getElementById('panel-' + name);
    if (tab)   tab.classList.add('active');
    if (panel) panel.classList.add('active');
}

// ── Save / Password ───────────────────────────────────────
function saveProfile() {
    var name = document.getElementById('editName').value.trim();
    var email = document.getElementById('editEmail').value.trim();
    if (!name) { toastr.error('Name is required'); return; }
    PROFILE_USER.name  = name;
    PROFILE_USER.email = email;
    fillUserInfo();
    switchTab('info');
    toastr.success('Profile updated successfully');
}

function changePassword() {
    var cur  = document.getElementById('curPw').value;
    var nw   = document.getElementById('newPw').value;
    var conf = document.getElementById('confPw').value;
    if (!cur || !nw || !conf) { toastr.error('Please fill all password fields'); return; }
    if (nw !== conf) { toastr.error('New passwords do not match'); return; }
    if (nw.length < 6) { toastr.error('Password must be at least 6 characters'); return; }
    ['curPw','newPw','confPw'].forEach(function (id) { document.getElementById(id).value = ''; });
    toastr.success('Password changed successfully');
}

// ── Helpers ───────────────────────────────────────────────
function setText(id, val) { var el = document.getElementById(id); if (el) el.textContent = val; }
function setVal(id, val)  { var el = document.getElementById(id); if (el) el.value = val; }
