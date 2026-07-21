var initialPage = (typeof EXPENSES_PAGE_DATA !== 'undefined' ? EXPENSES_PAGE_DATA : { page: { items: [], page: 1, pageSize: 8, totalCount: 0 }, allTimeTotalAmount: 0, todayTotalAmount: 0, allTimeCount: 0 });
var categories = (typeof EXP_CATEGORIES !== 'undefined' ? EXP_CATEGORIES : []);
var vendors    = (typeof EXP_VENDORS    !== 'undefined' ? EXP_VENDORS    : []);
var accounts   = (typeof EXP_ACCOUNTS   !== 'undefined' ? EXP_ACCOUNTS   : []);
var budgetSummary = (typeof EXP_BUDGET_SUMMARY !== 'undefined' ? EXP_BUDGET_SUMMARY : []);

var currentPage = initialPage.page.page, pageSize = initialPage.page.pageSize, searchQuery = '', catF = '', methodF = '';
var currentPageItems = initialPage.page.items, currentTotalCount = initialPage.page.totalCount;
var headerStats = { allTimeTotalAmount: initialPage.allTimeTotalAmount, todayTotalAmount: initialPage.todayTotalAmount, allTimeCount: initialPage.allTimeCount };
var editingExpenseId = null;
var searchDebounceHandle = null;

function fetchPage() {
    var params = new URLSearchParams({ page: currentPage, pageSize: pageSize });
    if (searchQuery) params.set('search', searchQuery);
    if (catF) params.set('category', catF);
    if (methodF) params.set('paymentMethod', methodF);

    return fetch('/Expenses/GetPage?' + params.toString())
        .then(function (res) { return res.json(); })
        .then(function (result) {
            currentPageItems = result.page.items;
            currentTotalCount = result.page.totalCount;
            headerStats = { allTimeTotalAmount: result.allTimeTotalAmount, todayTotalAmount: result.todayTotalAmount, allTimeCount: result.allTimeCount };
            renderTable();
        });
}

function renderBudgetSummary() {
    var row = document.getElementById('budgetSummaryRow');
    if (!row) return;
    if (!budgetSummary.length) {
        row.innerHTML = '<div class="col-12 text-muted" style="font-size:.85rem;">No active payment accounts configured yet.</div>';
        return;
    }
    row.innerHTML = budgetSummary.map(function (b) {
        var noCap = !b.monthlyBudgetEstimate;
        var over = !noCap && b.remainingThisMonth < 0;
        var color = noCap ? '#475569' : (over ? '#dc2626' : '#0d9488');
        var remainingText = noCap ? 'No cap set' : b.remainingThisMonth.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' left';
        return '<div class="col-6 col-md-3">'
            + '<div class="gp-stat-card">'
            + '<div style="font-size:.72rem;font-weight:700;color:#94a3b8;text-transform:uppercase;letter-spacing:.06em;">' + escapeHtml(b.accountName) + '</div>'
            + '<div class="gp-stat-val" style="font-size:1.15rem;color:' + color + ';">' + remainingText + '</div>'
            + (noCap ? '' : '<div style="font-size:.72rem;color:#94a3b8;">' + b.spentThisMonth.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' spent of ' + b.monthlyBudgetEstimate.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + '</div>')
            + '</div></div>';
    }).join('');
}
// "Unassigned" is the placeholder DbExpensesApiClient.GenerateDueRecurringExpensesAsync stamps on
// every auto-generated row (recurring schedules have no account of their own) -- an amber "Needs
// Review" badge instead of the raw enum-ish string makes those rows obvious at a glance so staff
// know to open and complete them (picking a real payment method/account triggers budget checks).
var methodLabel = { Cash: 'Cash', CreditCard: 'Credit Card', BankTransfer: 'Bank Transfer', WalletCredit: 'Wallet Credit', Unassigned: 'Needs Review' };
var methodBadge = { Cash: 'gp-badge-green', CreditCard: 'gp-badge-blue', BankTransfer: 'gp-badge-purple', WalletCredit: 'gp-badge-teal', Unassigned: 'gp-badge-amber' };

function renderTable() {
    var pages = Math.ceil(currentTotalCount / pageSize) || 1;
    document.getElementById('expensesTableBody').innerHTML = currentPageItems.length ? currentPageItems.map(function (e, i) { return `
        <tr>
            <td>${(currentPage-1)*pageSize+i+1}</td>
            <td><span class="badge" style="background:#f1f5f9;color:#475569;border-radius:8px;font-size:.75rem;font-weight:700;">${escapeHtml(e.expenseCategoryName)}</span></td>
            <td><span style="font-weight:700;color:#dc2626;">${e.amount.toLocaleString()}</span></td>
            <td style="font-size:.82rem;color:#64748b;">${escapeHtml(e.title)}</td>
            <td>${e.expenseDate}</td>
            <td>${escapeHtml(e.vendorName || '—')}</td>
            <td><span class="gp-badge ${methodBadge[e.paymentMethod]||'gp-badge-gray'}">${methodLabel[e.paymentMethod]||e.paymentMethod}</span></td>
            <td style="font-size:.82rem;color:#64748b;">${escapeHtml(e.accountName || '—')}</td>
            <td style="font-size:.8rem;color:#94a3b8;">${escapeHtml(e.receiptNumber || '—')}</td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${e.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteExpense(${e.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="10" class="text-center py-4 text-muted">No expenses found</td></tr>';
    document.getElementById('pageInfo').textContent = `Showing ${currentPageItems.length} of ${currentTotalCount}`;
    var btns = document.getElementById('pageBtns');
    btns.innerHTML = '';
    for (var p = 1; p <= pages; p++) {
        var btn = document.createElement('button');
        btn.className = 'gp-page-btn' + (p===currentPage?' active':'');
        btn.textContent = p;
        btn.onclick = (function(pp){ return function(){ currentPage=pp; fetchPage(); }; })(p);
        btns.appendChild(btn);
    }

    document.getElementById('statMonthly').textContent = headerStats.allTimeTotalAmount.toLocaleString();
    document.getElementById('statToday').textContent = headerStats.todayTotalAmount.toLocaleString();
    document.getElementById('statCount').textContent = headerStats.allTimeCount;
}

function populateFilters() {
    document.getElementById('catFilter').innerHTML = '<option value="">All Categories</option>' +
        categories.map(function (c) { return `<option>${escapeHtml(c.categoryName)}</option>`; }).join('');
}

function populateModalSelects() {
    document.getElementById('fCategory').innerHTML = categories.map(function (c) {
        return `<option value="${c.id}">${escapeHtml(c.categoryName)}</option>`;
    }).join('');
    document.getElementById('fVendor').innerHTML = '<option value="">— None —</option>' +
        vendors.map(function (v) { return `<option value="${v.id}">${escapeHtml(v.vendorName)}</option>`; }).join('');
    document.getElementById('fAccount').innerHTML = '<option value="">— None —</option>' +
        accounts.map(function (a) { return `<option value="${a.id}">${escapeHtml(a.name)}</option>`; }).join('');
}

function openModal(id) {
    populateModalSelects();
    editingExpenseId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Expense' : 'Add Expense';
    if (id) {
        var e = currentPageItems.find(function (x) { return x.id === id; });
        document.getElementById('fCategory').value = e.expenseCategoryId;
        document.getElementById('fAmount').value = e.amount;
        document.getElementById('fTitle').value = e.title;
        document.getElementById('fReceipt').value = e.receiptNumber || '';
        document.getElementById('fVendor').value = e.vendorId || '';
        document.getElementById('fMethod').value = e.paymentMethod;
        document.getElementById('fAccount').value = e.accountId || '';
        document.getElementById('fDate').value = e.expenseDate;
    } else {
        document.getElementById('fAmount').value = '';
        document.getElementById('fTitle').value = '';
        document.getElementById('fReceipt').value = '';
        document.getElementById('fAccount').value = '';
        document.getElementById('fMethod').value = 'Cash';
        document.getElementById('fDate').value = new Date().toISOString().slice(0, 10);
    }
    new bootstrap.Modal(document.getElementById('expenseModal')).show();
}

function saveExpense() {
    var categoryId = parseInt(document.getElementById('fCategory').value, 10);
    var amount = parseFloat(document.getElementById('fAmount').value);
    var title = document.getElementById('fTitle').value.trim();
    if (!categoryId || isNaN(amount) || !title) { toastr.error('Category, amount and title are required'); return; }

    var vendorVal = document.getElementById('fVendor').value;
    var accountVal = document.getElementById('fAccount').value;
    var body = {
        expenseCategoryId: categoryId,
        vendorId: vendorVal ? parseInt(vendorVal, 10) : null,
        title: title,
        amount: amount,
        expenseDate: document.getElementById('fDate').value,
        paymentMethod: document.getElementById('fMethod').value,
        receiptNumber: document.getElementById('fReceipt').value.trim() || null,
        notes: null,
        accountId: accountVal ? parseInt(accountVal, 10) : null
    };

    var url = editingExpenseId ? '/Expenses/Update?id=' + editingExpenseId : '/Expenses/Create';
    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save expense'); return; }
        toastr.success('Expense recorded');
        bootstrap.Modal.getInstance(document.getElementById('expenseModal')).hide();
        if (!editingExpenseId) currentPage = 1;
        fetchPage();
        refreshBudgetSummary();
    });
}

function deleteExpense(id) {
    confirmDelete('This expense will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/Expenses/Delete?id=' + id, { method: 'POST' })
            .then(function (res) { return res.json().then(function (data) { return { ok: res.ok, data: data }; }); })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete expense'); return; }
                deletedAlert('Expense deleted.');
                if (currentPageItems.length === 1 && currentPage > 1) currentPage--;
                fetchPage();
                refreshBudgetSummary();
            });
    });
}

function refreshBudgetSummary() {
    fetch('/Expenses/BudgetSummary').then(function (res) { return res.json(); }).then(function (data) {
        budgetSummary = data;
        renderBudgetSummary();
    });
}

function generateRecurring() {
    var btn = document.getElementById('btnGenerateRecurring');
    if (btn) btn.disabled = true;
    fetch('/Expenses/GenerateRecurring', { method: 'POST' })
        .then(function (res) { return res.json().then(function (data) { return { ok: res.ok, data: data }; }); })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not generate expenses'); return; }
            if (result.data.generated > 0) {
                toastr.success(result.data.generated + ' expense(s) generated from due schedules');
                currentPage = 1;
                fetchPage();
                refreshBudgetSummary();
            } else {
                toastr.info('No schedules are due right now');
            }
        })
        .finally(function () { if (btn) btn.disabled = false; });
}

function handleSearch(v) {
    searchQuery = v;
    currentPage = 1;
    clearTimeout(searchDebounceHandle);
    searchDebounceHandle = setTimeout(fetchPage, 300);
}
function handleFilter() {
    catF = document.getElementById('catFilter').value;
    methodF = document.getElementById('methodFilter').value;
    currentPage = 1;
    fetchPage();
}

document.addEventListener('DOMContentLoaded', function () {
    populateFilters();
    renderTable();
    renderBudgetSummary();
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
