var initialPage = (typeof INVOICES_PAGE_DATA !== 'undefined' ? INVOICES_PAGE_DATA : { items: [], page: 1, pageSize: 8, totalCount: 0 });

var currentPage = initialPage.page, pageSize = initialPage.pageSize, searchQuery = '';
var currentPageItems = initialPage.items, currentTotalCount = initialPage.totalCount;
var searchDebounceHandle = null;
var statusBadge = { Paid: 'gp-badge-green', Partial: 'gp-badge-yellow', Unpaid: 'gp-badge-red' };

function fetchPage() {
    var params = new URLSearchParams({ page: currentPage, pageSize: pageSize });
    if (searchQuery) params.set('search', searchQuery);

    return fetch('/Payments/GetInvoicesPage?' + params.toString())
        .then(function (res) { return res.json(); })
        .then(function (result) {
            currentPageItems = result.items;
            currentTotalCount = result.totalCount;
            renderTable();
        });
}

function renderTable() {
    var pages = Math.ceil(currentTotalCount / pageSize) || 1;
    document.getElementById('invoicesTableBody').innerHTML = currentPageItems.length ? currentPageItems.map(function (inv, i) { return `
        <tr>
            <td>${(currentPage-1)*pageSize+i+1}</td>
            <td><span style="font-weight:700;color:#0d9488;">#${inv.invoiceId}</span></td>
            <td><span style="font-weight:700;">${escapeHtml(inv.patientName || 'Walk-in')}</span></td>
            <td>${inv.netAmount.toLocaleString()}</td>
            <td style="color:#16a34a;font-weight:600;">${inv.paidAmount.toLocaleString()}</td>
            <td style="color:${inv.balanceDue>0?'#dc2626':'#16a34a'};font-weight:600;">${inv.balanceDue.toLocaleString()}</td>
            <td><span class="gp-badge ${statusBadge[inv.paymentStatus]||'gp-badge-gray'}">${inv.paymentStatus}</span></td>
            <td>${new Date(inv.invoiceDate).toLocaleDateString()}</td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-view" onclick="viewInvoice(${inv.invoiceId})"><i class="ri-eye-line"></i></button>
                <button class="gp-btn-icon gp-btn-print" onclick="printInvoice(${inv.invoiceId})"><i class="ri-printer-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="9" class="text-center py-4 text-muted">No outstanding invoices</td></tr>';
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
}

function viewInvoice(id) {
    fetch('/Payments/GetInvoice?id=' + id)
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not load invoice'); return; }
            renderInvoice(result.data);
            new bootstrap.Modal(document.getElementById('invoiceModal')).show();
        });
}

function invoiceBrandMarkup() {
    var clinicName = (typeof REPORT_CLINIC_NAME !== 'undefined' && REPORT_CLINIC_NAME) ? REPORT_CLINIC_NAME : 'ClinicMS';
    var logoUrl = (typeof REPORT_LOGO_URL !== 'undefined') ? REPORT_LOGO_URL : null;
    if (logoUrl) {
        return '<img src="' + escapeHtml(logoUrl) + '" alt="' + escapeHtml(clinicName) + '" class="invoice-brand-logo">';
    }
    return '<div class="invoice-brand">' + escapeHtml(clinicName) + '<span>Clinic Management System</span></div>';
}

function renderInvoice(inv) {
    document.getElementById('invoicePrintArea').innerHTML = `
        <div class="invoice-print">
            <div class="invoice-header">
                <div>${invoiceBrandMarkup()}</div>
                <div class="invoice-no">
                    <h3>${escapeHtml(inv.invoiceNumber)}</h3>
                    <small>Issue Date: ${new Date(inv.invoiceDate).toLocaleDateString()}</small>
                </div>
            </div>
            <div class="invoice-parties">
                <div class="invoice-party">
                    <h6>Billed To</h6>
                    <p style="font-weight:800;color:#1e293b;">${escapeHtml(inv.patientName || 'Walk-in Customer')}</p>
                </div>
                <div class="invoice-party" style="text-align:right;">
                    <h6>Status</h6>
                    <p style="font-weight:800;color:#0d9488;">${inv.paymentStatus}</p>
                </div>
            </div>
            <table class="invoice-table">
                <thead><tr><th>Description</th><th>Qty</th><th>Unit Price</th><th>Total</th></tr></thead>
                <tbody>${inv.items.map(function (item) {
                    var desc = item.serviceName || item.productName || item.itemType;
                    return `<tr><td>${escapeHtml(desc)}</td><td>${item.quantity}</td><td>${item.unitPrice.toLocaleString()}</td><td>${item.totalPrice.toLocaleString()}</td></tr>`;
                }).join('')}</tbody>
            </table>
            <div class="invoice-totals">
                <table>
                    <tr><td>Subtotal</td><td>${inv.totalAmount.toLocaleString()}</td></tr>
                    <tr><td>Discount</td><td>-${inv.discountAmount.toLocaleString()}</td></tr>
                    <tr><td>VAT</td><td>${inv.vatAmount.toLocaleString()}</td></tr>
                    <tr><td>Amount Paid</td><td style="color:#16a34a;">${inv.paidAmount.toLocaleString()}</td></tr>
                    <tr class="total-row"><td>Balance Due</td><td>${inv.balanceDue.toLocaleString()}</td></tr>
                </table>
            </div>
            <div style="margin-top:24px;padding-top:16px;border-top:1px solid #f1f5f9;font-size:.78rem;color:#94a3b8;text-align:center;">
                Thank you. Generated by ClinicMS.
            </div>
        </div>`;
}

function printInvoice(id) {
    fetch('/Payments/GetInvoice?id=' + id)
        .then(function (res) { return res.json(); })
        .then(function (inv) {
            renderInvoice(inv);
            new bootstrap.Modal(document.getElementById('invoiceModal')).show();
            setTimeout(function () { window.print(); }, 500);
        });
}

function handleSearch(v) {
    searchQuery = v;
    currentPage = 1;
    clearTimeout(searchDebounceHandle);
    searchDebounceHandle = setTimeout(fetchPage, 300);
}
document.addEventListener('DOMContentLoaded', renderTable);

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
