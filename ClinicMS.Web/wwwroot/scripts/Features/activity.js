var initialPage = (typeof ACTIVITY_PAGE_DATA !== 'undefined' ? ACTIVITY_PAGE_DATA : {
    feed: { items: [], page: 1, pageSize: 12, totalCount: 0 },
    stats: { totalCount: 0, createCount: 0, updateCount: 0, deleteCount: 0, loginCount: 0, totalSparkline: [], createSparkline: [], updateSparkline: [], deleteSparkline: [], topUsers: [] }
});

var currentPage = initialPage.feed.page, pageSize = initialPage.feed.pageSize;
var currentFeedItems = initialPage.feed.items, currentTotalCount = initialPage.feed.totalCount;
var currentStats = initialPage.stats;
var activeFilter = 'all';
var searchDebounceHandle = null;

var USER_COLORS = ['#0d9488', '#7c3aed', '#0891b2', '#d97706', '#dc2626', '#16a34a', '#9333ea'];
function colorForUser(name) {
    var hash = 0;
    for (var i = 0; i < name.length; i++) hash = (hash * 31 + name.charCodeAt(i)) >>> 0;
    return USER_COLORS[hash % USER_COLORS.length];
}

function getActionClass(a) {
    var m = { CREATE: 'create', UPDATE: 'update', DELETE: 'delete', LOGIN: 'login' };
    return 'al-action-' + (m[a] || 'view');
}

function formatDate(iso) {
    var months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    var d = new Date(iso);
    return `${d.getDate()} ${months[d.getMonth()]}`;
}
function formatTime(iso) {
    return new Date(iso).toTimeString().slice(0, 5);
}

function fetchPage() {
    var search = document.getElementById('logSearch').value || '';
    var params = new URLSearchParams({ page: currentPage, pageSize: pageSize });
    if (search) params.set('search', search);
    if (activeFilter !== 'all') params.set('action', activeFilter);

    return fetch('/Activity/GetPage?' + params.toString())
        .then(function (res) { return res.json(); })
        .then(function (result) {
            currentFeedItems = result.feed.items;
            currentTotalCount = result.feed.totalCount;
            currentStats = result.stats;
            renderAll();
        });
}

function renderAll() {
    renderFeed();
    updateKPIs();
    renderDistribution();
    renderTopUsers();
}

// ── Feed ───────────────────────────────────────────────────
function renderFeed() {
    var pages = Math.ceil(currentTotalCount / pageSize) || 1;
    var feed = document.getElementById('activityFeed');

    if (!currentFeedItems.length) {
        feed.innerHTML = '<div style="padding:40px;text-align:center;color:#94a3b8;font-size:.875rem;">No activity found</div>';
    } else {
        feed.innerHTML = currentFeedItems.map(function (l) {
            var parts = l.userName.split(' ');
            var init = (parts[0] || '').charAt(0);
            var initLast = (parts[1] || '').charAt(0);
            var color = colorForUser(l.userName);
            return `
            <div class="al-feed-item">
                <div class="al-feed-avatar" style="background:${color}">${escapeHtml(init)}${escapeHtml(initLast)}</div>
                <div class="al-feed-body">
                    <div>
                        <span class="al-feed-user">${escapeHtml(l.userName)}</span>
                        <span class="al-feed-ip">· ${escapeHtml(l.ipAddress || '—')}</span>
                    </div>
                    <div class="al-feed-msg">
                        <span class="al-action-badge ${getActionClass(l.action)}">${l.action}</span>
                        ${escapeHtml(l.message)}
                    </div>
                </div>
                <div class="al-feed-time">
                    <div class="al-feed-date">${formatDate(l.createdAt)}</div>
                    <div class="al-feed-hour">${formatTime(l.createdAt)}</div>
                </div>
            </div>`;
        }).join('');
    }

    var start = currentTotalCount ? (currentPage - 1) * pageSize + 1 : 0;
    var end = Math.min(currentPage * pageSize, currentTotalCount);
    document.getElementById('feedPageInfo').textContent = `Showing ${start}–${end} of ${currentTotalCount}`;

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
    var pages = Math.ceil(currentTotalCount / pageSize) || 1;
    if (p < 1 || p > pages) return;
    currentPage = p;
    fetchPage();
}

function filterLogs() {
    currentPage = 1;
    clearTimeout(searchDebounceHandle);
    searchDebounceHandle = setTimeout(fetchPage, 300);
}

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.al-filter-tab').forEach(function (btn) {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.al-filter-tab').forEach(function (b) { b.classList.remove('active'); });
            this.classList.add('active');
            activeFilter = this.dataset.filter;
            currentPage = 1;
            fetchPage();
        });
    });
});

// ── KPI Counts ─────────────────────────────────────────────
function buildSparkline(containerId, values, color) {
    var el = document.getElementById(containerId);
    if (!el) return;
    var max = Math.max.apply(null, values.concat([1]));
    el.innerHTML = values.map(function (v) {
        var h = Math.max(4, Math.round((v / max) * 32));
        return `<div class="al-spark-bar" style="background:${color};height:${h}px;opacity:${0.4 + 0.6*(v/max)}"></div>`;
    }).join('');
}

function updateKPIs() {
    document.getElementById('kpiTotal').textContent = currentStats.totalCount.toLocaleString();
    document.getElementById('kpiCreate').textContent = currentStats.createCount;
    document.getElementById('kpiUpdate').textContent = currentStats.updateCount;
    document.getElementById('kpiDelete').textContent = currentStats.deleteCount;

    buildSparkline('sparkTotal', currentStats.totalSparkline, '#0d9488');
    buildSparkline('sparkCreate', currentStats.createSparkline, '#16a34a');
    buildSparkline('sparkUpdate', currentStats.updateSparkline, '#d97706');
    buildSparkline('sparkDelete', currentStats.deleteSparkline, '#dc2626');
}

// ── Distribution Donut Chart ────────────────────────────────
var distChartInstance = null;
function renderDistribution() {
    var counts = {
        CREATE: currentStats.createCount,
        UPDATE: currentStats.updateCount,
        DELETE: currentStats.deleteCount,
        LOGIN: currentStats.loginCount
    };
    var total = Object.values(counts).reduce(function (a, b) { return a + b; }, 0) || 1;
    var colors = { CREATE: '#16a34a', UPDATE: '#d97706', DELETE: '#dc2626', LOGIN: '#0d9488' };

    var ctx = document.getElementById('distChart');
    if (!ctx) return;

    if (distChartInstance) distChartInstance.destroy();
    distChartInstance = new Chart(ctx, {
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

    document.querySelector('.al-donut-center .dv').textContent = currentStats.totalCount.toLocaleString();

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
    var el = document.getElementById('topUsersList');
    if (!el) return;
    var max = (currentStats.topUsers[0] && currentStats.topUsers[0].count) || 1;

    el.innerHTML = currentStats.topUsers.map(function (entry) {
        var initParts = entry.userName.split(' ').map(function (w) { return w[0]; }).slice(0, 2);
        var init = initParts.join('');
        var color = colorForUser(entry.userName);
        var pct = ((entry.count / max) * 100).toFixed(0);
        return `
        <div class="al-dist-row">
            <div class="al-feed-avatar" style="background:${color};width:28px;height:28px;border-radius:8px;font-size:.65rem;font-weight:800;color:#fff;display:flex;align-items:center;justify-content:center;flex-shrink:0;">${escapeHtml(init)}</div>
            <div style="flex:1;min-width:0;">
                <div style="font-size:.8rem;font-weight:700;color:#1e293b;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${escapeHtml(entry.userName)}</div>
                <div style="height:4px;background:#f1f5f9;border-radius:4px;margin-top:5px;overflow:hidden;"><div style="height:100%;background:${color};width:${pct}%;border-radius:4px;"></div></div>
            </div>
            <div style="font-size:.8rem;font-weight:800;color:#475569;padding-left:10px;">${entry.count}</div>
        </div>`;
    }).join('');
}

// ── Export (fetches every row matching the current search/filter, not just this page) ──
function exportLogs() {
    var search = document.getElementById('logSearch').value || '';
    var params = new URLSearchParams({ page: 1, pageSize: 1000 });
    if (search) params.set('search', search);
    if (activeFilter !== 'all') params.set('action', activeFilter);

    fetch('/Activity/GetPage?' + params.toString())
        .then(function (res) { return res.json(); })
        .then(function (result) {
            var logs = result.feed.items;
            if (!logs.length) { toastr.warning('Nothing to export'); return; }

            var rows = [['Date', 'User', 'Action', 'Message', 'IP Address']];
            logs.forEach(function (l) {
                rows.push([l.createdAt, l.userName, l.action, l.message, l.ipAddress || '']);
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
        });
}

// ── Init ───────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    renderAll();
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
});
