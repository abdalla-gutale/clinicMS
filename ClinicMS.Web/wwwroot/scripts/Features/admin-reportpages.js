var reportPages = (typeof REPORT_PAGES_DATA !== 'undefined' ? REPORT_PAGES_DATA : []);
var reportModules = (typeof REPORT_MODULES_DATA !== 'undefined' ? REPORT_MODULES_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', editingId = null;

function filtered() {
    var q = searchQuery.toLowerCase();
    return reportPages.filter(function (r) {
        return !q || r.reportName.toLowerCase().includes(q) || r.reportUrl.toLowerCase().includes(q);
    }).sort(function (a, b) { return a.displayOrder - b.displayOrder; });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('reportPagesTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (r, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${r.reportName}</span></td>
            <td><span class="gp-badge gp-badge-teal">${r.moduleName}</span></td>
            <td style="color:#64748b;">${r.reportUrl}</td>
            <td style="color:#64748b;">${r.displayOrder}</td>
            <td><span class="gp-badge ${r.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${r.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${r.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteReportPage(${r.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="7" class="text-center py-4 text-muted">No report pages found</td></tr>';
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

function populateModuleSelect(selectedId) {
    var sel = document.getElementById('fModule');
    sel.innerHTML = reportModules.map(function (m) {
        return `<option value="${m.id}" ${m.id === selectedId ? 'selected' : ''}>${m.moduleName}</option>`;
    }).join('');
}

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Report Page' : 'Add Report Page';
    if (id) {
        var r = reportPages.find(function (x) { return x.id === id; });
        document.getElementById('fReportName').value = r.reportName;
        populateModuleSelect(r.moduleId);
        document.getElementById('fReportUrl').value = r.reportUrl;
        document.getElementById('fDisplayOrder').value = r.displayOrder;
        document.getElementById('fActive').checked = r.isActive;
    } else {
        document.getElementById('fReportName').value = '';
        populateModuleSelect(reportModules.length ? reportModules[0].id : null);
        document.getElementById('fReportUrl').value = '';
        document.getElementById('fDisplayOrder').value = 1;
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('reportPageModal')).show();
}

function saveReportPage() {
    var reportName = document.getElementById('fReportName').value.trim();
    var moduleId = parseInt(document.getElementById('fModule').value, 10);
    var reportUrl = document.getElementById('fReportUrl').value.trim();
    var displayOrder = parseInt(document.getElementById('fDisplayOrder').value, 10);
    var isActive = document.getElementById('fActive').checked;

    if (!reportName) { toastr.error('Report name is required'); return; }
    if (!moduleId) { toastr.error('Module is required'); return; }
    if (!reportUrl || reportUrl[0] !== '/') { toastr.error('Report URL must start with /'); return; }
    if (isNaN(displayOrder) || displayOrder < 1) { toastr.error('Enter a valid display order'); return; }

    var body = { moduleId: moduleId, reportName: reportName, reportUrl: reportUrl, displayOrder: displayOrder, isActive: isActive };
    var url = editingId ? '/Administration/UpdateReportPage?id=' + editingId : '/Administration/CreateReportPage';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save report page'); return; }
        if (editingId) {
            var idx = reportPages.findIndex(function (r) { return r.id === editingId; });
            if (idx >= 0) reportPages[idx] = result.data;
            toastr.success('Report page updated');
        } else {
            reportPages.push(result.data);
            toastr.success('Report page added');
        }
        bootstrap.Modal.getInstance(document.getElementById('reportPageModal')).hide();
        renderTable();
    });
}

function deleteReportPage(id) {
    if (!confirm('Delete this report page?')) return;
    fetch('/Administration/DeleteReportPage?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete report page'); return; }
            reportPages = reportPages.filter(function (r) { return r.id !== id; });
            toastr.success('Deleted');
            renderTable();
        });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
