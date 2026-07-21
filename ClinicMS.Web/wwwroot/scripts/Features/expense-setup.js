// ── Tab switching ──────────────────────────────────────────
function switchTab(paneId) {
    document.querySelectorAll('.es-tab').forEach(function (btn) { btn.classList.toggle('active', btn.dataset.pane === paneId); });
    document.querySelectorAll('.es-pane').forEach(function (pane) { pane.classList.toggle('active', pane.id === paneId); });
    setTimeout(initSelect2, 50);
}

// ── Shared decimal-input helpers (used by Budget Estimation + Setup Expenses) ──
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

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });

// ══════════════════════════════════════════════════════════
// TAB: Expense Categories
// ══════════════════════════════════════════════════════════
var categories = (typeof CATEGORIES_DATA !== 'undefined' ? CATEGORIES_DATA : []);
var categoryTotals = (typeof CATEGORY_TOTALS !== 'undefined' ? CATEGORY_TOTALS : {});
var catPage = 1, catPerPage = 10, catSearch = '', catEditingId = null;

function catFiltered() {
    var q = catSearch.toLowerCase();
    return categories.filter(function (c) {
        return !q || c.categoryName.toLowerCase().includes(q) || (c.description || '').toLowerCase().includes(q);
    });
}

function renderCategoriesTable() {
    var data = catFiltered();
    var total = data.length;
    var pages = Math.ceil(total / catPerPage) || 1;
    if (catPage > pages) catPage = 1;
    var slice = data.slice((catPage - 1) * catPerPage, catPage * catPerPage);
    document.getElementById('catsTableBody').innerHTML = slice.length ? slice.map(function (c, i) { return `
        <tr>
            <td>${(catPage - 1) * catPerPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(c.categoryName)}</span></td>
            <td style="color:#64748b;">${escapeHtml(c.description || '')}</td>
            <td><span style="font-weight:700;color:#dc2626;">${(categoryTotals[c.id] || 0).toLocaleString()}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openCategoryModal(${c.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteCategory(${c.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="5" class="text-center py-4 text-muted">No categories found</td></tr>';
    document.getElementById('catPageInfo').textContent = `Showing ${slice.length} of ${total}`;
    var btns = document.getElementById('catPageBtns');
    btns.innerHTML = '';
    for (var p = 1; p <= pages; p++) {
        var btn = document.createElement('button');
        btn.className = 'gp-page-btn' + (p === catPage ? ' active' : '');
        btn.textContent = p;
        btn.onclick = (function (pp) { return function () { catPage = pp; renderCategoriesTable(); }; })(p);
        btns.appendChild(btn);
    }
}

function openCategoryModal(id) {
    catEditingId = id || null;
    document.getElementById('catModalTitle').textContent = id ? 'Edit Category' : 'Add Category';
    if (id) {
        var c = categories.find(function (x) { return x.id === id; });
        document.getElementById('fCatName').value = c.categoryName;
        document.getElementById('fCatDesc').value = c.description || '';
    } else {
        document.getElementById('fCatName').value = '';
        document.getElementById('fCatDesc').value = '';
    }
    new bootstrap.Modal(document.getElementById('catModal')).show();
}

function saveCategory() {
    var name = document.getElementById('fCatName').value.trim();
    if (!name) { toastr.error('Name required'); return; }
    var desc = document.getElementById('fCatDesc').value.trim();
    var body = { categoryName: name, description: desc || null, isActive: true };
    var url = catEditingId ? '/Expenses/UpdateCategory?id=' + catEditingId : '/Expenses/CreateCategory';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save category'); return; }
        if (catEditingId) {
            var idx = categories.findIndex(function (c) { return c.id === catEditingId; });
            if (idx >= 0) categories[idx] = result.data;
            toastr.success('Category updated');
        } else {
            categories.push(result.data);
            toastr.success('Category added');
        }
        bootstrap.Modal.getInstance(document.getElementById('catModal')).hide();
        renderCategoriesTable();
    });
}

function deleteCategory(id) {
    confirmDelete('This expense category will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/Expenses/DeleteCategory?id=' + id, { method: 'POST' })
            .then(function (res) { return res.json().then(function (data) { return { ok: res.ok, data: data }; }); })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete category'); return; }
                categories = categories.filter(function (c) { return c.id !== id; });
                deletedAlert('Category deleted.');
                renderCategoriesTable();
            });
    });
}

function handleCategorySearch(v) { catSearch = v; catPage = 1; renderCategoriesTable(); }

// ══════════════════════════════════════════════════════════
// TAB: Vendors
// ══════════════════════════════════════════════════════════
var vendors = (typeof VENDORS_DATA !== 'undefined' ? VENDORS_DATA : []);
var vendorPage = 1, vendorPerPage = 8, vendorSearch = '', vendorEditingId = null;

function vendorFiltered() {
    var q = vendorSearch.toLowerCase();
    return vendors.filter(function (v) {
        return !q || v.vendorName.toLowerCase().includes(q) || (v.contactPerson || '').toLowerCase().includes(q);
    });
}

function renderVendorsTable() {
    var data = vendorFiltered();
    var total = data.length;
    var pages = Math.ceil(total / vendorPerPage) || 1;
    if (vendorPage > pages) vendorPage = 1;
    var slice = data.slice((vendorPage - 1) * vendorPerPage, vendorPage * vendorPerPage);
    document.getElementById('vendorsTableBody').innerHTML = slice.length ? slice.map(function (v, i) { return `
        <tr>
            <td>${(vendorPage - 1) * vendorPerPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(v.vendorName)}</span></td>
            <td>${escapeHtml(v.contactPerson || '')}</td>
            <td style="color:#64748b;">${escapeHtml(v.phone || '')}</td>
            <td style="color:#64748b;">${escapeHtml(v.email || '')}</td>
            <td><span class="gp-badge ${v.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${v.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openVendorModal(${v.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteVendor(${v.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="7" class="text-center py-4 text-muted">No vendors found</td></tr>';
    document.getElementById('vendorPageInfo').textContent = `Showing ${slice.length} of ${total}`;
    var btns = document.getElementById('vendorPageBtns');
    btns.innerHTML = '';
    for (var p = 1; p <= pages; p++) {
        var btn = document.createElement('button');
        btn.className = 'gp-page-btn' + (p === vendorPage ? ' active' : '');
        btn.textContent = p;
        btn.onclick = (function (pp) { return function () { vendorPage = pp; renderVendorsTable(); }; })(p);
        btns.appendChild(btn);
    }
}

function openVendorModal(id) {
    vendorEditingId = id || null;
    document.getElementById('vendorModalTitle').textContent = id ? 'Edit Vendor' : 'Add Vendor';
    if (id) {
        var v = vendors.find(function (x) { return x.id === id; });
        document.getElementById('fVendorName').value = v.vendorName;
        document.getElementById('fContactPerson').value = v.contactPerson || '';
        document.getElementById('fVendorPhone').value = v.phone || '';
        document.getElementById('fVendorEmail').value = v.email || '';
        document.getElementById('fVendorActive').checked = v.isActive;
    } else {
        document.getElementById('fVendorName').value = '';
        document.getElementById('fContactPerson').value = '';
        document.getElementById('fVendorPhone').value = '';
        document.getElementById('fVendorEmail').value = '';
        document.getElementById('fVendorActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('vendorModal')).show();
}

function saveVendor() {
    var vendorName = document.getElementById('fVendorName').value.trim();
    if (!vendorName) { toastr.error('Vendor name is required'); return; }
    var email = document.getElementById('fVendorEmail').value.trim();
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { toastr.error('Enter a valid email address'); return; }

    var body = {
        vendorName: vendorName,
        contactPerson: document.getElementById('fContactPerson').value.trim() || null,
        phone: document.getElementById('fVendorPhone').value.trim() || null,
        email: email || null,
        isActive: document.getElementById('fVendorActive').checked
    };
    var url = vendorEditingId ? '/SupplyChain/UpdateVendor?id=' + vendorEditingId : '/SupplyChain/CreateVendor';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save vendor'); return; }
        if (vendorEditingId) {
            var idx = vendors.findIndex(function (v) { return v.id === vendorEditingId; });
            if (idx >= 0) vendors[idx] = result.data;
            toastr.success('Vendor updated');
        } else {
            vendors.push(result.data);
            toastr.success('Vendor added');
        }
        bootstrap.Modal.getInstance(document.getElementById('vendorModal')).hide();
        renderVendorsTable();
    });
}

function deleteVendor(id) {
    confirmDelete('This vendor will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/SupplyChain/DeleteVendor?id=' + id, { method: 'POST' })
            .then(function (res) { return res.json().then(function (data) { return { ok: res.ok, data: data }; }); })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete vendor'); return; }
                vendors = vendors.filter(function (v) { return v.id !== id; });
                deletedAlert('Vendor deleted.');
                renderVendorsTable();
            });
    });
}

function handleVendorSearch(v) { vendorSearch = v; vendorPage = 1; renderVendorsTable(); }

// ══════════════════════════════════════════════════════════
// TAB: Budget Estimation (per payment account, monthly)
// ══════════════════════════════════════════════════════════
var paymentAccounts = (typeof PAYMENT_ACCOUNTS_DATA !== 'undefined' ? PAYMENT_ACCOUNTS_DATA : []);
var budgetSummary = (typeof BUDGET_SUMMARY_DATA !== 'undefined' ? BUDGET_SUMMARY_DATA : []);
var budgetEditingAccountId = null;
var BUDGET_ACCOUNT_TYPE_LABELS = { Cash: 'Cash', Merchant: 'Merchant', MasterCard: 'Master Card' };
var BUDGET_ACCOUNT_TYPE_SUB_LABELS = { None: '—', Evc: 'EVC', Zaad: 'ZAAD', Somtel: 'SOMTEL' };

function renderBudgetTable() {
    var tbody = document.getElementById('budgetTableBody');
    if (!paymentAccounts.length) {
        tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4 text-muted">No payment accounts configured yet</td></tr>';
        return;
    }
    tbody.innerHTML = paymentAccounts.map(function (a, i) {
        var summary = budgetSummary.find(function (b) { return b.accountId === a.id; });
        var spent = summary ? summary.spentThisMonth : 0;
        var remaining = summary ? summary.remainingThisMonth : a.monthlyBudgetEstimate;
        var noCap = !a.monthlyBudgetEstimate;
        var remainingColor = noCap ? '#64748b' : (remaining < 0 ? '#dc2626' : '#15803d');
        return `
        <tr>
            <td>${i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(a.name)}</span></td>
            <td>${escapeHtml(BUDGET_ACCOUNT_TYPE_LABELS[a.accountType] || a.accountType)}</td>
            <td>${escapeHtml(BUDGET_ACCOUNT_TYPE_SUB_LABELS[a.accountTypeSub] || a.accountTypeSub || '—')}</td>
            <td>
                <input type="text" inputmode="decimal" autocomplete="off" class="form-control form-control-sm" style="width:120px;"
                    value="${a.monthlyBudgetEstimate || ''}" placeholder="0 = no cap"
                    onkeypress="return isDecimalKeyAllowed(event)" oninput="sanitizeDecimal(this)"
                    onkeydown="if(event.key==='Enter'){event.preventDefault();this.blur();}"
                    onblur="saveInlineBudgetEstimate(${a.id}, this)">
            </td>
            <td style="color:#dc2626;">${spent.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
            <td style="font-weight:700;color:${remainingColor};">${noCap ? '—' : remaining.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
            <td>
                <div class="form-check form-switch mb-0">
                    <input class="form-check-input" type="checkbox" ${a.isActive ? 'checked' : ''} style="width:38px;height:20px;cursor:pointer;" onchange="toggleBudgetAccountStatus(${a.id}, this)">
                </div>
            </td>
            <td><button class="gp-btn-icon gp-btn-edit" onclick="openBudgetModal(${a.id})" title="Edit"><i class="ri-pencil-line"></i></button></td>
        </tr>`;
    }).join('');
}

function toggleBudgetAccountStatus(accountId, checkbox) {
    var account = paymentAccounts.find(function (x) { return x.id === accountId; });
    if (!account) return;
    var body = {
        name: account.name,
        accountType: account.accountType,
        accountTypeSub: account.accountTypeSub,
        number: account.number,
        monthlyBudgetEstimate: account.monthlyBudgetEstimate,
        isActive: checkbox.checked
    };
    fetch('/Settings/UpdatePaymentAccount?id=' + accountId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not update status'); checkbox.checked = account.isActive; return; }
        var idx = paymentAccounts.findIndex(function (x) { return x.id === accountId; });
        if (idx >= 0) paymentAccounts[idx] = result.data;
        toastr.success(result.data.isActive ? 'Account turned on' : 'Account turned off');
    });
}

function saveInlineBudgetEstimate(accountId, input) {
    var account = paymentAccounts.find(function (x) { return x.id === accountId; });
    if (!account) return;

    var raw = input.value.trim();
    var amount = raw ? parseFloat(raw) : 0;
    if (isNaN(amount) || amount < 0) {
        toastr.error('Enter a valid monthly estimate (0 for no cap)');
        input.value = account.monthlyBudgetEstimate || '';
        return;
    }
    if (amount === account.monthlyBudgetEstimate) return;

    var body = {
        name: account.name,
        accountType: account.accountType,
        accountTypeSub: account.accountTypeSub,
        number: account.number,
        monthlyBudgetEstimate: amount,
        isActive: account.isActive
    };

    fetch('/Settings/UpdatePaymentAccount?id=' + accountId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not update the monthly estimate'); input.value = account.monthlyBudgetEstimate || ''; return; }
        var idx = paymentAccounts.findIndex(function (x) { return x.id === accountId; });
        if (idx >= 0) paymentAccounts[idx] = result.data;
        toastr.success('Monthly estimate updated');
        renderBudgetTable();
        refreshBudgetSummary();
    });
}

// Master Card is always a plain card number; Cash/Merchant only need a number when tied to a
// mobile-money carrier (Account Type Sub != None) so the payment can be traced to a real wallet.
function toggleBudgetNumberField() {
    var type = document.getElementById('fBudgetAccountType').value;
    var subGroup = document.getElementById('budgetTypeSubGroup');
    var sub = document.getElementById('fBudgetAccountTypeSub').value;

    if (type === 'MasterCard') {
        subGroup.style.display = 'none';
        document.getElementById('fBudgetAccountTypeSub').value = 'None';
        document.getElementById('budgetNumberGroup').style.display = '';
    } else {
        subGroup.style.display = '';
        document.getElementById('budgetNumberGroup').style.display = sub === 'None' ? 'none' : '';
    }
}

function openBudgetModal(accountId) {
    budgetEditingAccountId = accountId || null;
    document.getElementById('budgetModalTitle').textContent = accountId ? 'Edit Payment Account' : 'Add Payment Account';
    if (accountId) {
        var a = paymentAccounts.find(function (x) { return x.id === accountId; });
        document.getElementById('fBudgetAccountName').value = a.name;
        document.getElementById('fBudgetAccountType').value = a.accountType;
        document.getElementById('fBudgetAccountTypeSub').value = a.accountTypeSub || 'None';
        document.getElementById('fBudgetAccountNumber').value = a.number || '';
        document.getElementById('fBudgetAmount').value = a.monthlyBudgetEstimate || '';
        document.getElementById('fBudgetActive').checked = a.isActive;
    } else {
        document.getElementById('fBudgetAccountName').value = '';
        document.getElementById('fBudgetAccountType').value = 'Cash';
        document.getElementById('fBudgetAccountTypeSub').value = 'None';
        document.getElementById('fBudgetAccountNumber').value = '';
        document.getElementById('fBudgetAmount').value = '';
        document.getElementById('fBudgetActive').checked = true;
    }
    toggleBudgetNumberField();
    new bootstrap.Modal(document.getElementById('budgetModal')).show();
}

function saveBudgetEstimate() {
    var name = document.getElementById('fBudgetAccountName').value.trim();
    if (!name) { toastr.error('Account name is required'); return; }

    var accountType = document.getElementById('fBudgetAccountType').value;
    var accountTypeSub = accountType === 'MasterCard' ? 'None' : document.getElementById('fBudgetAccountTypeSub').value;
    var number = document.getElementById('fBudgetAccountNumber').value.trim() || null;
    var numberRequired = accountType === 'MasterCard' || accountTypeSub !== 'None';
    if (numberRequired && !number) { toastr.error('Enter an Account No for ' + (accountType === 'MasterCard' ? 'Master Card' : accountTypeSub) + ' accounts'); return; }

    var amountRaw = document.getElementById('fBudgetAmount').value.trim();
    var amount = amountRaw ? parseFloat(amountRaw) : 0;
    if (isNaN(amount) || amount < 0) { toastr.error('Enter a valid monthly estimate (0 for no cap)'); return; }

    var body = {
        name: name,
        accountType: accountType,
        accountTypeSub: accountTypeSub,
        number: number,
        monthlyBudgetEstimate: amount,
        isActive: document.getElementById('fBudgetActive').checked
    };

    var url = budgetEditingAccountId ? '/Settings/UpdatePaymentAccount?id=' + budgetEditingAccountId : '/Settings/CreatePaymentAccount';
    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save the payment account'); return; }
        if (budgetEditingAccountId) {
            var idx = paymentAccounts.findIndex(function (x) { return x.id === budgetEditingAccountId; });
            if (idx >= 0) paymentAccounts[idx] = result.data;
            toastr.success('Payment account updated');
        } else {
            paymentAccounts.push(result.data);
            toastr.success('Payment account added');
        }
        bootstrap.Modal.getInstance(document.getElementById('budgetModal')).hide();
        renderBudgetTable();
        refreshBudgetSummary();
    });
}

function refreshBudgetSummary() {
    fetch('/Expenses/BudgetSummary').then(function (res) { return res.json(); }).then(function (data) {
        budgetSummary = data;
        renderBudgetTable();
    });
}

// ══════════════════════════════════════════════════════════
// TAB: Setup Expenses (recurring schedules)
// ══════════════════════════════════════════════════════════
var schedules = (typeof RECURRING_EXPENSES_DATA !== 'undefined' ? RECURRING_EXPENSES_DATA : []);
var schedPage = 1, schedPerPage = 8, schedSearch = '', schedStatusF = '', schedEditingId = null;

function schedFiltered() {
    var q = schedSearch.toLowerCase();
    return schedules.filter(function (s) {
        var match = !q || s.title.toLowerCase().includes(q) || s.expenseCategoryName.toLowerCase().includes(q);
        var st = !schedStatusF || (s.isActive ? 'Active' : 'Inactive') === schedStatusF;
        return match && st;
    });
}

function renderSchedulesTable() {
    var data = schedFiltered();
    var total = data.length;
    var pages = Math.ceil(total / schedPerPage) || 1;
    if (schedPage > pages) schedPage = 1;
    var slice = data.slice((schedPage - 1) * schedPerPage, schedPage * schedPerPage);
    var tbody = document.getElementById('schedulesTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (s, i) { return `
        <tr>
            <td>${(schedPage - 1) * schedPerPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(s.title)}</span></td>
            <td><span class="gp-badge gp-badge-teal">${escapeHtml(s.expenseCategoryName)}</span></td>
            <td style="color:#64748b;">${escapeHtml(s.vendorName || '—')}</td>
            <td style="font-weight:700;color:#0d9488;">${s.amount.toLocaleString()}</td>
            <td>${s.frequency}</td>
            <td style="color:#64748b;">${new Date(s.nextDueDate).toLocaleDateString()}</td>
            <td><span class="gp-badge ${s.autoGenerate ? 'gp-badge-green' : 'gp-badge-amber'}">${s.autoGenerate ? 'Yes' : 'No'}</span></td>
            <td><span class="gp-badge ${s.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${s.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openScheduleModal(${s.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteSchedule(${s.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="10" class="text-center py-4 text-muted">No recurring expenses found</td></tr>';
    document.getElementById('schedPageInfo').textContent = `Showing ${slice.length} of ${total}`;
    var btns = document.getElementById('schedPageBtns');
    btns.innerHTML = '';
    for (var p = 1; p <= pages; p++) {
        var btn = document.createElement('button');
        btn.className = 'gp-page-btn' + (p === schedPage ? ' active' : '');
        btn.textContent = p;
        btn.onclick = (function (pp) { return function () { schedPage = pp; renderSchedulesTable(); }; })(p);
        btns.appendChild(btn);
    }
}

function populateScheduleCategorySelect(selectedId) {
    var sel = document.getElementById('fSchedCategory');
    sel.innerHTML = categories.filter(function (c) { return c.isActive; }).map(function (c) {
        return `<option value="${c.id}" ${c.id === selectedId ? 'selected' : ''}>${escapeHtml(c.categoryName)}</option>`;
    }).join('');
}

function populateScheduleVendorSelect(selectedId) {
    var sel = document.getElementById('fSchedVendor');
    sel.innerHTML = '<option value="">None</option>' + vendors.filter(function (v) { return v.isActive; }).map(function (v) {
        return `<option value="${v.id}" ${v.id === selectedId ? 'selected' : ''}>${escapeHtml(v.vendorName)}</option>`;
    }).join('');
}

function openScheduleModal(id) {
    schedEditingId = id || null;
    document.getElementById('schedModalTitle').textContent = id ? 'Edit Schedule' : 'Add Schedule';
    if (id) {
        var s = schedules.find(function (x) { return x.id === id; });
        document.getElementById('fSchedTitle').value = s.title;
        populateScheduleCategorySelect(s.expenseCategoryId);
        populateScheduleVendorSelect(s.vendorId);
        document.getElementById('fSchedAmount').value = s.amount;
        document.getElementById('fSchedFrequency').value = s.frequency;
        document.getElementById('fSchedNextDueDate').value = s.nextDueDate;
        document.getElementById('fSchedAutoGenerate').checked = s.autoGenerate;
        document.getElementById('fSchedActive').checked = s.isActive;
    } else {
        document.getElementById('fSchedTitle').value = '';
        populateScheduleCategorySelect(categories.length ? categories[0].id : null);
        populateScheduleVendorSelect(null);
        document.getElementById('fSchedAmount').value = '';
        document.getElementById('fSchedFrequency').value = 'Monthly';
        document.getElementById('fSchedNextDueDate').value = '';
        document.getElementById('fSchedAutoGenerate').checked = true;
        document.getElementById('fSchedActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('scheduleModal')).show();
}

function saveSchedule() {
    var expenseCategoryId = parseInt(document.getElementById('fSchedCategory').value, 10);
    var vendorRaw = document.getElementById('fSchedVendor').value;
    var vendorId = vendorRaw ? parseInt(vendorRaw, 10) : null;
    var title = document.getElementById('fSchedTitle').value.trim();
    var amount = parseFloat(document.getElementById('fSchedAmount').value);
    var frequency = document.getElementById('fSchedFrequency').value;
    var nextDueDate = document.getElementById('fSchedNextDueDate').value;
    var autoGenerate = document.getElementById('fSchedAutoGenerate').checked;
    var isActive = document.getElementById('fSchedActive').checked;

    if (!title) { toastr.error('Title is required'); return; }
    if (!expenseCategoryId) { toastr.error('Category is required'); return; }
    if (isNaN(amount) || amount <= 0) { toastr.error('Enter a valid amount'); return; }
    if (!nextDueDate) { toastr.error('Next due date is required'); return; }

    var body = { expenseCategoryId: expenseCategoryId, vendorId: vendorId, title: title, amount: amount, frequency: frequency, nextDueDate: nextDueDate, autoGenerate: autoGenerate, isActive: isActive };
    var url = schedEditingId ? '/Expenses/UpdateRecurringExpense?id=' + schedEditingId : '/Expenses/CreateRecurringExpense';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save schedule'); return; }
        if (schedEditingId) {
            var idx = schedules.findIndex(function (s) { return s.id === schedEditingId; });
            if (idx >= 0) schedules[idx] = result.data;
            toastr.success('Schedule updated');
        } else {
            schedules.push(result.data);
            toastr.success('Schedule added');
        }
        bootstrap.Modal.getInstance(document.getElementById('scheduleModal')).hide();
        renderSchedulesTable();
    });
}

function deleteSchedule(id) {
    confirmDelete('This recurring expense schedule will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/Expenses/DeleteRecurringExpense?id=' + id, { method: 'POST' })
            .then(function (res) { return res.json().then(function (data) { return { ok: res.ok, data: data }; }); })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete schedule'); return; }
                schedules = schedules.filter(function (s) { return s.id !== id; });
                deletedAlert('Schedule deleted.');
                renderSchedulesTable();
            });
    });
}

function handleScheduleSearch(v) { schedSearch = v; schedPage = 1; renderSchedulesTable(); }
function handleScheduleFilter() { schedStatusF = document.getElementById('schedStatusFilter').value; schedPage = 1; renderSchedulesTable(); }

// ── Init ───────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    renderCategoriesTable();
    renderVendorsTable();
    renderBudgetTable();
    renderSchedulesTable();
    initSelect2();
});
