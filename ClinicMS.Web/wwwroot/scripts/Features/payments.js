var initialPage = (typeof PAYMENTS_PAGE_DATA !== 'undefined' ? PAYMENTS_PAGE_DATA : { items: [], page: 1, pageSize: 8, totalCount: 0 });
var paymentAccounts = (typeof PAYMENT_ACCOUNTS_DATA !== 'undefined' ? PAYMENT_ACCOUNTS_DATA : []);
var accountBreakdown = (typeof ACCOUNT_BREAKDOWN_DATA !== 'undefined' ? ACCOUNT_BREAKDOWN_DATA : []);
var allPatients = (typeof ALL_PATIENTS_DATA !== 'undefined' ? ALL_PATIENTS_DATA : []);

function renderAccountBreakdown() {
    var tbody = document.getElementById('accountBreakdownBody');
    if (!tbody) return;
    tbody.innerHTML = accountBreakdown.length ? accountBreakdown.map(function (a) {
        var netColor = a.net >= 0 ? '#15803d' : '#dc2626';
        return `<tr>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(a.accountName)}</span></td>
            <td style="color:#15803d;">${a.totalIncome.toLocaleString()}</td>
            <td style="color:#dc2626;">${a.totalExpense.toLocaleString()}</td>
            <td style="font-weight:700;color:${netColor};">${a.net.toLocaleString()}</td>
        </tr>`;
    }).join('') : '<tr><td colspan="4" class="text-center py-3 text-muted">No account activity yet</td></tr>';
}

var currentPage = initialPage.page, pageSize = initialPage.pageSize, searchQuery = '';
var currentPageItems = initialPage.items, currentTotalCount = initialPage.totalCount;
var searchDebounceHandle = null;
var statusBadge = { Paid: 'gp-badge-green', Partial: 'gp-badge-orange', Unpaid: 'gp-badge-red' };

function fetchPage() {
    var params = new URLSearchParams({ page: currentPage, pageSize: pageSize });
    if (searchQuery) params.set('search', searchQuery);

    return fetch('/Payments/GetOutstandingPage?' + params.toString())
        .then(function (res) { return res.json(); })
        .then(function (result) {
            currentPageItems = result.items;
            currentTotalCount = result.totalCount;
            renderTable();
        });
}

function renderTable() {
    var pages = Math.ceil(currentTotalCount / pageSize) || 1;
    document.getElementById('paymentsTableBody').innerHTML = currentPageItems.length ? currentPageItems.map(function (o, i) { return `
        <tr>
            <td>${(currentPage-1)*pageSize+i+1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(o.patientName || 'Walk-in')}</span></td>
            <td>${new Date(o.invoiceDate).toLocaleDateString()}</td>
            <td>${o.netAmount.toLocaleString()}</td>
            <td>${o.paidAmount.toLocaleString()}</td>
            <td><span style="font-weight:700;color:#dc2626;">${o.balanceDue.toLocaleString()}</span></td>
            <td><span class="gp-badge ${statusBadge[o.paymentStatus]||'gp-badge-gray'}">${o.paymentStatus}</span></td>
            <td><button class="gp-btn-icon" style="background:#f0fdfa;color:#0d9488;" title="Record Payment"
                        onclick="openModal(${o.invoiceId}, ${o.patientId}, ${o.balanceDue})">
                <i class="ri-money-dollar-circle-line"></i>
            </button></td>
        </tr>`; }).join('') : '<tr><td colspan="8" class="text-center py-4 text-muted">No outstanding balances</td></tr>';
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

    document.getElementById('statTotal').textContent = (typeof MONTH_TOTAL !== 'undefined' ? MONTH_TOTAL : 0).toLocaleString();
    document.getElementById('statToday').textContent = (typeof TODAY_TOTAL !== 'undefined' ? TODAY_TOTAL : 0).toLocaleString();
    document.getElementById('statPending').textContent = currentTotalCount;
}

function openModal(invoiceId, patientId, balanceDue) {
    document.getElementById('fInvoiceId').value = invoiceId || '';
    var patientSel = document.getElementById('fPatient');
    // The record-payment patient dropdown lists every patient (not just those with an outstanding
    // balance) since this modal doubles as a general "record a payment" entry point.
    patientSel.innerHTML = allPatients.map(function (p) {
        return `<option value="${p.id}" ${p.id === patientId ? 'selected' : ''}>${escapeHtml(p.fullName)}</option>`;
    }).join('');
    document.getElementById('fAmount').value = balanceDue || '';
    document.getElementById('fReference').value = '';
    document.getElementById('fAccount').innerHTML = '<option value="">— None —</option>' +
        paymentAccounts.map(function (a) { return `<option value="${a.id}">${escapeHtml(a.name)}</option>`; }).join('');
    new bootstrap.Modal(document.getElementById('paymentModal')).show();
}

function savePayment() {
    var patientId = parseInt(document.getElementById('fPatient').value, 10);
    var amount = parseFloat(document.getElementById('fAmount').value);
    if (!patientId || isNaN(amount) || amount <= 0) { toastr.error('A patient and a valid amount are required'); return; }

    var invoiceIdVal = document.getElementById('fInvoiceId').value;
    var accountVal = document.getElementById('fAccount').value;
    var body = {
        invoiceId: invoiceIdVal ? parseInt(invoiceIdVal, 10) : null,
        patientId: patientId,
        amountPaid: amount,
        paymentMethod: document.getElementById('fMethod').value,
        referenceNumber: document.getElementById('fReference').value.trim() || null,
        accountId: accountVal ? parseInt(accountVal, 10) : null
    };

    fetch('/Payments/RecordPayment', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not record payment'); return; }
        toastr.success('Payment recorded');
        bootstrap.Modal.getInstance(document.getElementById('paymentModal')).hide();
        setTimeout(function () { window.location.reload(); }, 800);
    });
}

function handleSearch(v) {
    searchQuery = v;
    currentPage = 1;
    clearTimeout(searchDebounceHandle);
    searchDebounceHandle = setTimeout(fetchPage, 300);
}

document.addEventListener('DOMContentLoaded', function () { renderTable(); renderAccountBreakdown(); });

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
