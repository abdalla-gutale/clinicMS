var auditTrail = (typeof AUDIT_TRAIL_DATA !== 'undefined' ? AUDIT_TRAIL_DATA : []);
var userLogs = (typeof USER_LOGS_DATA !== 'undefined' ? USER_LOGS_DATA : []);
var usernamesById = (typeof USERNAMES_BY_ID !== 'undefined' ? USERNAMES_BY_ID : {});

var USER_COLORS = ['#0d9488', '#7c3aed', '#0891b2', '#d97706', '#dc2626', '#16a34a', '#9333ea'];
function colorForUser(name) {
    var hash = 0;
    for (var i = 0; i < name.length; i++) hash = (hash * 31 + name.charCodeAt(i)) >>> 0;
    return USER_COLORS[hash % USER_COLORS.length];
}

var USER_LOG_ACTION_MSG = {
    Login_Success: 'Signed in successfully',
    Login_Failed: 'Failed login attempt',
    Logout: 'Logged out',
    Session_Expired: 'Session expired'
};

// ── Build a unified feed from the real audit trail + user logs ──
var LOG_DATA = (function () {
    var logs = [];

    auditTrail.forEach(function (a) {
        var name = a.userId && usernamesById[a.userId] ? usernamesById[a.userId] : 'System';
        logs.push({
            user: { name: name, role: '', color: colorForUser(name) },
            action: a.action === 'INSERT' ? 'CREATE' : a.action,
            msg: `${a.action === 'INSERT' ? 'Created' : a.action === 'UPDATE' ? 'Updated' : 'Deleted'} ${a.tableName} #${a.recordId}`,
            ip: a.ipAddress || '—',
            date: new Date(a.createdAt)
        });
    });

    userLogs.forEach(function (l) {
        logs.push({
            user: { name: l.username, role: '', color: colorForUser(l.username) },
            action: 'LOGIN',
            msg: USER_LOG_ACTION_MSG[l.action] || l.action,
            ip: l.ipAddress || '—',
            date: new Date(l.createdAt)
        });
    });

    logs.sort(function (a, b) { return b.date - a.date; });
    return logs;
})();

// ── State ──────────────────────────────────────────────────
var activeFilter = 'all';
var currentPage = 1;
var PAGE_SIZE = 12;

// ── Sparkline ─────────────────────────────────────────────
function buildSparkline(containerId, values, color) {
    var el = document.getElementById(containerId);
    if (!el) return;
    var max = Math.max.apply(null, values.concat([1]));
    el.innerHTML = values.map(function (v) {
        var h = Math.max(4, Math.round((v / max) * 32));
        return `<div class="al-spark-bar" style="background:${color};height:${h}px;opacity:${0.4 + 0.6*(v/max)}"></div>`;
    }).join('');
}

function computeSparkData(actionFilter) {
    var weeks = 12;
    var result = new Array(weeks).fill(0);
    var now = new Date();
    LOG_DATA.forEach(function (l) {
        if (actionFilter !== 'all' && l.action.toLowerCase() !== actionFilter) return;
        var diff = (now - l.date) / (7 * 24 * 3600 * 1000);
        var idx = Math.floor(diff);
        if (idx >= 0 && idx < weeks) result[weeks - 1 - idx]++;
    });
    return result;
}

// ── KPI Counts ─────────────────────────────────────────────
function updateKPIs() {
    var total = LOG_DATA.length;
    var create = LOG_DATA.filter(function (l) { return l.action === 'CREATE'; }).length;
    var update = LOG_DATA.filter(function (l) { return l.action === 'UPDATE'; }).length;
    var del = LOG_DATA.filter(function (l) { return l.action === 'DELETE'; }).length;

    document.getElementById('kpiTotal').textContent = total.toLocaleString();
    document.getElementById('kpiCreate').textContent = create;
    document.getElementById('kpiUpdate').textContent = update;
    document.getElementById('kpiDelete').textContent = del;

    buildSparkline('sparkTotal', computeSparkData('all'), '#0d9488');
    buildSparkline('sparkCreate', computeSparkData('create'), '#16a34a');
    buildSparkline('sparkUpdate', computeSparkData('update'), '#d97706');
    buildSparkline('sparkDelete', computeSparkData('delete'), '#dc2626');
}

// ── Feed ───────────────────────────────────────────────────
function getActionClass(a) {
    var m = { CREATE: 'create', UPDATE: 'update', DELETE: 'delete', LOGIN: 'login' };
    return 'al-action-' + (m[a] || 'view');
}

function formatDate(d) {
    var months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    return `${d.getDate()} ${months[d.getMonth()]}`;
}
function formatTime(d) {
    return d.toTimeString().slice(0,5);
}

function filteredLogs() {
    var search = (document.getElementById('logSearch') && document.getElementById('logSearch').value || '').toLowerCase();
    return LOG_DATA.filter(function (l) {
        var matchFilter = activeFilter === 'all' || l.action.toLowerCase() === activeFilter;
        var matchSearch = !search ||
            l.user.name.toLowerCase().includes(search) ||
            l.action.toLowerCase().includes(search) ||
            l.msg.toLowerCase().includes(search) ||
            l.ip.includes(search);
        return matchFilter && matchSearch;
    });
}

function renderFeed() {
    var logs = filteredLogs();
    var total = logs.length;
    var pages = Math.ceil(total / PAGE_SIZE);
    currentPage = Math.min(currentPage, pages || 1);

    var slice = logs.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);
    var feed = document.getElementById('activityFeed');

    if (!slice.length) {
        feed.innerHTML = '<div style="padding:40px;text-align:center;color:#94a3b8;font-size:.875rem;">No activity found</div>';
    } else {
        feed.innerHTML = slice.map(function (l) {
            var parts = l.user.name.split(' ');
            var init = (parts[0] || '').charAt(0);
            var initLast = (parts[1] || '').charAt(0);
            return `
            <div class="al-feed-item">
                <div class="al-feed-avatar" style="background:${l.user.color}">${init}${initLast}</div>
                <div class="al-feed-body">
                    <div>
                        <span class="al-feed-user">${l.user.name}</span>
                        <span class="al-feed-ip">· ${l.ip}</span>
                    </div>
                    <div class="al-feed-msg">
                        <span class="al-action-badge ${getActionClass(l.action)}">${l.action}</span>
                        ${l.msg}
                    </div>
                </div>
                <div class="al-feed-time">
                    <div class="al-feed-date">${formatDate(l.date)}</div>
                    <div class="al-feed-hour">${formatTime(l.date)}</div>
                </div>
            </div>`;
        }).join('');
    }

    var start = total ? (currentPage - 1) * PAGE_SIZE + 1 : 0;
    var end = Math.min(currentPage * PAGE_SIZE, total);
    document.getElementById('feedPageInfo').textContent = `Showing ${start}–${end} of ${total}`;

    var btnContainer = document.getElementById('feedPageBtns');
    var btnPages = [];
    if (pages <= 5) {
        for (var i = 1; i <= pages; i++) btnPages.push(i);
    } else {
        btnPages.push(1);
        if (currentPage > 3) btnPages.push('…');
        for (var j = Math.max(2, currentPage - 1); j <= Math.min(pages - 1, currentPage + 1); j++) btnPages.push(j);
        if (currentPage < pages - 2) btnPages.push('…');
        btnPages.push(pages);
    }

    btnContainer.innerHTML = [
        `<button class="al-page-btn" onclick="changePage(${currentPage - 1})" ${currentPage===1?'disabled':''}>‹</button>`
    ].concat(btnPages.map(function (p) {
        return p === '…'
            ? `<span class="al-page-btn" style="border:none;background:none;cursor:default">…</span>`
            : `<button class="al-page-btn ${p===currentPage?'active':''}" onclick="changePage(${p})">${p}</button>`;
    })).concat([
        `<button class="al-page-btn" onclick="changePage(${currentPage + 1})" ${currentPage===pages?'disabled':''}>›</button>`
    ]).join('');
}

function changePage(p) {
    var logs = filteredLogs();
    var pages = Math.ceil(logs.length / PAGE_SIZE);
    if (p < 1 || p > pages) return;
    currentPage = p;
    renderFeed();
}

function filterLogs() { currentPage = 1; renderFeed(); }

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.al-filter-tab').forEach(function (btn) {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.al-filter-tab').forEach(function (b) { b.classList.remove('active'); });
            this.classList.add('active');
            activeFilter = this.dataset.filter;
            currentPage = 1;
            renderFeed();
        });
    });
});

// ── Distribution Donut Chart ────────────────────────────────
function renderDistribution() {
    var counts = {
        CREATE: LOG_DATA.filter(function (l) { return l.action === 'CREATE'; }).length,
        UPDATE: LOG_DATA.filter(function (l) { return l.action === 'UPDATE'; }).length,
        DELETE: LOG_DATA.filter(function (l) { return l.action === 'DELETE'; }).length,
        LOGIN: LOG_DATA.filter(function (l) { return l.action === 'LOGIN'; }).length
    };
    var total = Object.values(counts).reduce(function (a, b) { return a + b; }, 0) || 1;
    var colors = { CREATE: '#16a34a', UPDATE: '#d97706', DELETE: '#dc2626', LOGIN: '#0d9488' };

    var ctx = document.getElementById('distChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: Object.keys(counts),
            datasets: [{ data: Object.values(counts), backgroundColor: Object.values(colors), borderWidth: 0, hoverOffset: 6 }]
        },
        options: {
            cutout: '70%',
            plugins: { legend: { display: false }, tooltip: {
                callbacks: {
                    label: function (c) { return ` ${c.label}: ${c.raw} (${((c.raw/total)*100).toFixed(1)}%)`; }
                }
            }},
            animation: { animateRotate: true, duration: 900 }
        }
    });

    document.querySelector('.al-donut-center .dv').textContent = LOG_DATA.length.toLocaleString();

    var legend = document.getElementById('distLegend');
    if (legend) {
        legend.innerHTML = Object.entries(counts).map(function (entry) {
            var k = entry[0], v = entry[1];
            var pct = ((v / total) * 100).toFixed(0);
            return `
            <div class="al-dist-row">
                <div class="al-dist-dot" style="background:${colors[k]}"></div>
                <div class="al-dist-lbl">${k}</div>
                <div class="al-dist-bar-wrap"><div class="al-dist-bar" style="width:${pct}%;background:${colors[k]}"></div></div>
                <div class="al-dist-pct">${pct}%</div>
            </div>`;
        }).join('');
    }
}

// ── Top Active Users ────────────────────────────────────────
function renderTopUsers() {
    var counts = {};
    LOG_DATA.forEach(function (l) {
        var key = l.user.name;
        counts[key] = counts[key] || { count: 0, user: l.user };
        counts[key].count++;
    });
    var sorted = Object.values(counts).sort(function (a, b) { return b.count - a.count; }).slice(0, 5);
    var max = (sorted[0] && sorted[0].count) || 1;

    var el = document.getElementById('topUsersList');
    if (!el) return;
    el.innerHTML = sorted.map(function (entry) {
        var initParts = entry.user.name.split(' ').map(function (w) { return w[0]; }).slice(0, 2);
        var init = initParts.join('');
        var pct = ((entry.count / max) * 100).toFixed(0);
        return `
        <div class="al-dist-row">
            <div class="al-feed-avatar" style="background:${entry.user.color};width:28px;height:28px;border-radius:8px;font-size:.65rem;font-weight:800;color:#fff;display:flex;align-items:center;justify-content:center;flex-shrink:0;">${init}</div>
            <div style="flex:1;min-width:0;">
                <div style="font-size:.8rem;font-weight:700;color:#1e293b;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${entry.user.name}</div>
                <div style="height:4px;background:#f1f5f9;border-radius:4px;margin-top:5px;overflow:hidden;"><div style="height:100%;background:${entry.user.color};width:${pct}%;border-radius:4px;"></div></div>
            </div>
            <div style="font-size:.8rem;font-weight:800;color:#475569;padding-left:10px;">${entry.count}</div>
        </div>`;
    }).join('');
}

// ── Export (real CSV of the currently filtered logs) ────────
function exportLogs() {
    var logs = filteredLogs();
    if (!logs.length) { toastr.warning('Nothing to export'); return; }

    var rows = [['Date', 'User', 'Action', 'Message', 'IP Address']];
    logs.forEach(function (l) {
        rows.push([l.date.toISOString(), l.user.name, l.action, l.msg, l.ip]);
    });
    var csv = rows.map(function (r) {
        return r.map(function (v) { return `"${String(v).replace(/"/g, '""')}"`; }).join(',');
    }).join('\n');

    var blob = new Blob([csv], { type: 'text/csv' });
    var link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = 'activity-log-' + new Date().toISOString().slice(0, 10) + '.csv';
    link.click();
    toastr.success('Logs exported', 'Export', { timeOut: 2000 });
}

// ── Init ───────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    updateKPIs();
    renderFeed();
    renderDistribution();
    renderTopUsers();
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
});
