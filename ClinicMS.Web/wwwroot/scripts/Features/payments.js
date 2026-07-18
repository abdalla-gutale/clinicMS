var outstanding = (typeof OUTSTANDING_DATA !== 'undefined' ? OUTSTANDING_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '';
var statusBadge = { Paid: 'gp-badge-green', Partial: 'gp-badge-orange', Unpaid: 'gp-badge-red' };

function filtered() {
    var q = searchQuery.toLowerCase();
    return outstanding.filter(function (o) {
        return !q || (o.patientName || '').toLowerCase().includes(q);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage-1)*perPage, currentPage*perPage);
    document.getElementById('paymentsTableBody').innerHTML = slice.length ? slice.map(function (o, i) { return `
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${o.patientName || 'Walk-in'}</span></td>
            <td>${new Date(o.invoiceDate).toLocaleDateString()}</td>
            <td>${o.netAmount.toLocaleString()}</td>
            <td>${o.paidAmount.toLocaleString()}</td>
            <td><span style="font-weight:700;color:#dc2626;">${o.balanceDue.toLocaleString()}</span></td>
            <td><span class="gp-badge ${statusBadge[o.paymentStatus]||'gp-badge-gray'}">${o.paymentStatus}</span></td>
            <td><button class="gp-btn-icon" style="background:#f0fdfa;color:#0d9488;" title="Record Payment"
                        onclick="openModal(${o.invoiceId}, ${o.patientId}, '${(o.patientName||'').replace(/'/g,"\\'")}', ${o.balanceDue})">
                <i class="ri-money-dollar-circle-line"></i>
            </button></td>
        </tr>`; }).join('') : '<tr><td colspan="8" class="text-center py-4 text-muted">No outstanding balances</td></tr>';
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

    document.getElementById('statTotal').textContent = (typeof MONTH_TOTAL !== 'undefined' ? MONTH_TOTAL : 0).toLocaleString();
    document.getElementById('statToday').textContent = (typeof TODAY_TOTAL !== 'undefined' ? TODAY_TOTAL : 0).toLocaleString();
    document.getElementById('statPending').textContent = outstanding.length;
}

function openModal(invoiceId, patientId, patientName, balanceDue) {
    document.getElementById('fInvoiceId').value = invoiceId || '';
    var patientSel = document.getElementById('fPatient');
    var distinctPatients = [];
    var seen = {};
    outstanding.forEach(function (o) {
        if (o.patientId && !seen[o.patientId]) { seen[o.patientId] = true; distinctPatients.push({ id: o.patientId, name: o.patientName }); }
    });
    patientSel.innerHTML = distinctPatients.map(function (p) {
        return `<option value="${p.id}" ${p.id === patientId ? 'selected' : ''}>${p.name}</option>`;
    }).join('');
    document.getElementById('fAmount').value = balanceDue || '';
    document.getElementById('fReference').value = '';
    new bootstrap.Modal(document.getElementById('paymentModal')).show();
}

function savePayment() {
    var patientId = parseInt(document.getElementById('fPatient').value, 10);
    var amount = parseFloat(document.getElementById('fAmount').value);
    if (!patientId || isNaN(amount) || amount <= 0) { toastr.error('A patient and a valid amount are required'); return; }

    var invoiceIdVal = document.getElementById('fInvoiceId').value;
    var body = {
        invoiceId: invoiceIdVal ? parseInt(invoiceIdVal, 10) : null,
        patientId: patientId,
        amountPaid: amount,
        paymentMethod: document.getElementById('fMethod').value,
        referenceNumber: document.getElementById('fReference').value.trim() || null
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

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', renderTable);

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
    }
}
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
