var schedules = (typeof RECURRING_EXPENSES_DATA !== 'undefined' ? RECURRING_EXPENSES_DATA : []);
var scheduleCategories = (typeof EXPENSE_CATEGORIES_DATA !== 'undefined' ? EXPENSE_CATEGORIES_DATA : []);
var scheduleVendors = (typeof EXPENSE_VENDORS_DATA !== 'undefined' ? EXPENSE_VENDORS_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', statusF = '', editingId = null;

function isDecimalKeyAllowed(e) {
    if (e.ctrlKey || e.metaKey || e.altKey) return true;
    var ch = e.key;
    if (ch.length !== 1) return true;
    if (ch >= '0' && ch <= '9') return true;
    if (ch === '.' && e.target.value.indexOf('.') === -1) return true;
    e.preventDefault();
    return false;
}
function sanitizeDecimal(input) {
    var value = input.value.replace(/[^0-9.]/g, '');
    var firstDot = value.indexOf('.');
    if (firstDot !== -1) value = value.slice(0, firstDot + 1) + value.slice(firstDot + 1).replace(/\./g, '');
    input.value = value;
}

function filtered() {
    var q = searchQuery.toLowerCase();
    return schedules.filter(function (s) {
        var match = !q || s.title.toLowerCase().includes(q) || s.expenseCategoryName.toLowerCase().includes(q);
        var st = !statusF || (s.isActive ? 'Active' : 'Inactive') === statusF;
        return match && st;
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('schedulesTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (s, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${s.title}</span></td>
            <td><span class="gp-badge gp-badge-teal">${s.expenseCategoryName}</span></td>
            <td style="color:#64748b;">${s.vendorName || '—'}</td>
            <td style="font-weight:700;color:#0d9488;">${s.amount.toLocaleString()}</td>
            <td>${s.frequency}</td>
            <td style="color:#64748b;">${new Date(s.nextDueDate).toLocaleDateString()}</td>
            <td><span class="gp-badge ${s.autoGenerate ? 'gp-badge-green' : 'gp-badge-amber'}">${s.autoGenerate ? 'Yes' : 'No'}</span></td>
            <td><span class="gp-badge ${s.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${s.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${s.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteSchedule(${s.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="10" class="text-center py-4 text-muted">No recurring expenses found</td></tr>';
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

function populateCategorySelect(selectedId) {
    var sel = document.getElementById('fCategory');
    sel.innerHTML = scheduleCategories.map(function (c) {
        return `<option value="${c.id}" ${c.id === selectedId ? 'selected' : ''}>${c.categoryName}</option>`;
    }).join('');
}

function populateVendorSelect(selectedId) {
    var sel = document.getElementById('fVendor');
    sel.innerHTML = '<option value="">None</option>' + scheduleVendors.map(function (v) {
        return `<option value="${v.id}" ${v.id === selectedId ? 'selected' : ''}>${v.vendorName}</option>`;
    }).join('');
}

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Schedule' : 'Add Schedule';
    if (id) {
        var s = schedules.find(function (x) { return x.id === id; });
        document.getElementById('fTitle').value = s.title;
        populateCategorySelect(s.expenseCategoryId);
        populateVendorSelect(s.vendorId);
        document.getElementById('fAmount').value = s.amount;
        document.getElementById('fFrequency').value = s.frequency;
        document.getElementById('fNextDueDate').value = s.nextDueDate;
        document.getElementById('fAutoGenerate').checked = s.autoGenerate;
        document.getElementById('fActive').checked = s.isActive;
    } else {
        document.getElementById('fTitle').value = '';
        populateCategorySelect(scheduleCategories.length ? scheduleCategories[0].id : null);
        populateVendorSelect(null);
        document.getElementById('fAmount').value = '';
        document.getElementById('fFrequency').value = 'Monthly';
        document.getElementById('fNextDueDate').value = '';
        document.getElementById('fAutoGenerate').checked = true;
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('scheduleModal')).show();
}

function saveSchedule() {
    var expenseCategoryId = parseInt(document.getElementById('fCategory').value, 10);
    var vendorRaw = document.getElementById('fVendor').value;
    var vendorId = vendorRaw ? parseInt(vendorRaw, 10) : null;
    var title = document.getElementById('fTitle').value.trim();
    var amount = parseFloat(document.getElementById('fAmount').value);
    var frequency = document.getElementById('fFrequency').value;
    var nextDueDate = document.getElementById('fNextDueDate').value;
    var autoGenerate = document.getElementById('fAutoGenerate').checked;
    var isActive = document.getElementById('fActive').checked;

    if (!title) { toastr.error('Title is required'); return; }
    if (!expenseCategoryId) { toastr.error('Category is required'); return; }
    if (isNaN(amount) || amount <= 0) { toastr.error('Enter a valid amount'); return; }
    if (!nextDueDate) { toastr.error('Next due date is required'); return; }

    var body = { expenseCategoryId: expenseCategoryId, vendorId: vendorId, title: title, amount: amount, frequency: frequency, nextDueDate: nextDueDate, autoGenerate: autoGenerate, isActive: isActive };
    var url = editingId ? '/Expenses/UpdateRecurringExpense?id=' + editingId : '/Expenses/CreateRecurringExpense';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save schedule'); return; }
        if (editingId) {
            var idx = schedules.findIndex(function (s) { return s.id === editingId; });
            if (idx >= 0) schedules[idx] = result.data;
            toastr.success('Schedule updated');
        } else {
            schedules.push(result.data);
            toastr.success('Schedule added');
        }
        bootstrap.Modal.getInstance(document.getElementById('scheduleModal')).hide();
        renderTable();
    });
}

function deleteSchedule(id) {
    if (!confirm('Delete this recurring expense schedule?')) return;
    fetch('/Expenses/DeleteRecurringExpense?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete schedule'); return; }
            schedules = schedules.filter(function (s) { return s.id !== id; });
            toastr.success('Deleted');
            renderTable();
        });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() { statusF = document.getElementById('statusFilter').value; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () { renderTable(); });

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
