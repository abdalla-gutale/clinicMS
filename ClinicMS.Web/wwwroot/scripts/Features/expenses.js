var expenses   = (typeof EXPENSES_DATA  !== 'undefined' ? EXPENSES_DATA  : []);
var categories = (typeof EXP_CATEGORIES !== 'undefined' ? EXP_CATEGORIES : []);
var vendors    = (typeof EXP_VENDORS    !== 'undefined' ? EXP_VENDORS    : []);
var accounts   = (typeof EXP_ACCOUNTS   !== 'undefined' ? EXP_ACCOUNTS   : []);

var currentPage = 1, perPage = 8, searchQuery = '', catF = '', methodF = '';
var methodLabel = { Cash: 'Cash', CreditCard: 'Credit Card', BankTransfer: 'Bank Transfer', WalletCredit: 'Wallet Credit' };
var methodBadge = { Cash: 'gp-badge-green', CreditCard: 'gp-badge-blue', BankTransfer: 'gp-badge-purple', WalletCredit: 'gp-badge-teal' };

function filtered() {
    var q = searchQuery.toLowerCase();
    return expenses.filter(function (e) {
        var match = !q || e.title.toLowerCase().includes(q) || (e.vendorName || '').toLowerCase().includes(q) || e.expenseCategoryName.toLowerCase().includes(q);
        var c = !catF || e.expenseCategoryName === catF;
        var m = !methodF || e.paymentMethod === methodF;
        return match && c && m;
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage-1)*perPage, currentPage*perPage);
    document.getElementById('expensesTableBody').innerHTML = slice.length ? slice.map(function (e, i) { return `
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span class="badge" style="background:#f1f5f9;color:#475569;border-radius:8px;font-size:.75rem;font-weight:700;">${e.expenseCategoryName}</span></td>
            <td><span style="font-weight:700;color:#dc2626;">${e.amount.toLocaleString()}</span></td>
            <td style="font-size:.82rem;color:#64748b;">${e.title}</td>
            <td>${e.expenseDate}</td>
            <td>${e.vendorName || '—'}</td>
            <td><span class="gp-badge ${methodBadge[e.paymentMethod]||'gp-badge-gray'}">${methodLabel[e.paymentMethod]||e.paymentMethod}</span></td>
            <td style="font-size:.82rem;color:#64748b;">${e.accountName || '—'}</td>
            <td style="font-size:.8rem;color:#94a3b8;">${e.receiptNumber || '—'}</td>
        </tr>`; }).join('') : '<tr><td colspan="9" class="text-center py-4 text-muted">No expenses found</td></tr>';
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

    var monthly = expenses.reduce(function (a, e) { return a + e.amount; }, 0);
    document.getElementById('statMonthly').textContent = monthly.toLocaleString();
    var today = new Date().toISOString().slice(0, 10);
    var todayAmt = expenses.filter(function (e) { return e.expenseDate === today; }).reduce(function (a, e) { return a + e.amount; }, 0);
    document.getElementById('statToday').textContent = todayAmt.toLocaleString();
    document.getElementById('statCount').textContent = expenses.length;
}

function populateFilters() {
    document.getElementById('catFilter').innerHTML = '<option value="">All Categories</option>' +
        categories.map(function (c) { return `<option>${c.categoryName}</option>`; }).join('');
}

function populateModalSelects() {
    document.getElementById('fCategory').innerHTML = categories.map(function (c) {
        return `<option value="${c.id}">${c.categoryName}</option>`;
    }).join('');
    document.getElementById('fVendor').innerHTML = '<option value="">— None —</option>' +
        vendors.map(function (v) { return `<option value="${v.id}">${v.vendorName}</option>`; }).join('');
    document.getElementById('fAccount').innerHTML = '<option value="">— None —</option>' +
        accounts.map(function (a) { return `<option value="${a.id}">${a.name}</option>`; }).join('');
}

function openModal() {
    populateModalSelects();
    document.getElementById('fAmount').value = '';
    document.getElementById('fTitle').value = '';
    document.getElementById('fReceipt').value = '';
    document.getElementById('fAccount').value = '';
    document.getElementById('fDate').value = new Date().toISOString().slice(0, 10);
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

    fetch('/Expenses/Create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save expense'); return; }
        expenses.unshift(result.data);
        toastr.success('Expense recorded');
        bootstrap.Modal.getInstance(document.getElementById('expenseModal')).hide();
        renderTable();
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() {
    catF = document.getElementById('catFilter').value;
    methodF = document.getElementById('methodFilter').value;
    currentPage = 1; renderTable();
}

document.addEventListener('DOMContentLoaded', function () {
    populateFilters();
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
