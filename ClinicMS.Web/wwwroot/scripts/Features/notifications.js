var patients = (typeof PATIENTS_DATA !== 'undefined' ? PATIENTS_DATA : []);

// This session's real send attempts/results only -- there is no server-side notification log to load from.
var notifications = [];

var currentPage = 1, perPage = 8, searchQuery = '', typeF = '', statusF = '';
var typeBadge = { WhatsApp: 'gp-badge-teal', Email: 'gp-badge-blue' };
var statusBadge = { Sent: 'gp-badge-green', Failed: 'gp-badge-red' };

function filtered() {
    var q = searchQuery.toLowerCase();
    return notifications.filter(function (n) {
        return (!q || n.recipient.toLowerCase().includes(q) || n.title.toLowerCase().includes(q)) &&
            (!typeF || n.type === typeF) && (!statusF || n.status === statusF);
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total/perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage-1)*perPage, currentPage*perPage);
    document.getElementById('notifTableBody').innerHTML = slice.length ? slice.map(function (n, i) { return `
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span class="gp-badge ${typeBadge[n.type]||'gp-badge-gray'}">${n.type}</span></td>
            <td><span style="font-weight:600;">${escapeHtml(n.recipient)}</span></td>
            <td>${escapeHtml(n.title)}</td>
            <td class="msg-preview" style="font-size:.8rem;color:#64748b;">${escapeHtml(n.message)}</td>
            <td><span class="gp-badge ${statusBadge[n.status]||'gp-badge-gray'}">${n.status}</span></td>
            <td style="font-size:.78rem;color:#64748b;">${n.sentAt}</td>
        </tr>`; }).join('') : '<tr><td colspan="7" class="text-center py-4 text-muted">No notifications sent this session</td></tr>';
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
    document.getElementById('statSent').textContent = notifications.filter(function (n) { return n.status === 'Sent'; }).length;
    document.getElementById('statFailed').textContent = notifications.filter(function (n) { return n.status === 'Failed'; }).length;
    document.getElementById('statPending').textContent = 0;
}

function populateRecipients() {
    document.getElementById('fRecipients').innerHTML = patients.map(function (p) {
        return `<option value="${p.id}">${escapeHtml(p.fullName)}</option>`;
    }).join('');
}

function openModal() {
    document.getElementById('fTitle').value = '';
    document.getElementById('fMessage').value = '';
    new bootstrap.Modal(document.getElementById('notifModal')).show();
}

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('input[name="nType"]').forEach(function (radio) {
        radio.addEventListener('change', function () {
            document.getElementById('titleWrap').style.display = this.value === 'Email' ? '' : 'none';
        });
    });
});

function sendNotification() {
    var title = document.getElementById('fTitle').value.trim();
    var message = document.getElementById('fMessage').value.trim();
    var recipientIds = Array.from(document.getElementById('fRecipients').selectedOptions).map(function (o) { return parseInt(o.value, 10); });
    var type = document.querySelector('input[name="nType"]:checked').value;

    if (!message || !recipientIds.length) { toastr.error('Enter a message and select at least one recipient'); return; }
    if (type === 'Email' && !title) { toastr.error('Subject is required for Email'); return; }

    var sendBtn = document.getElementById('sendNotifBtn');
    sendBtn.disabled = true;

    var requests = recipientIds.map(function (patientId) {
        var patient = patients.find(function (p) { return p.id === patientId; });
        var url = type === 'Email' ? '/Notifications/SendEmail' : '/Notifications/SendWhatsApp';
        var body = type === 'Email'
            ? { patientId: patientId, templateId: null, subject: title, customMessage: message }
            : { patientId: patientId, templateId: null, customMessage: message };

        return fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        }).then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data, patient: patient }; });
        }).then(function (result) {
            notifications.unshift({
                type: type,
                recipient: result.patient ? result.patient.fullName : ('#' + patientId),
                title: title || '(WhatsApp message)',
                message: message,
                status: result.ok && result.data.sent ? 'Sent' : 'Failed',
                sentAt: new Date().toISOString().replace('T', ' ').slice(0, 16)
            });
        });
    });

    Promise.all(requests).then(function () {
        sendBtn.disabled = false;
        var sentCount = notifications.filter(function (n) { return n.status === 'Sent'; }).length;
        toastr[sentCount === recipientIds.length ? 'success' : 'warning']('Sent to ' + sentCount + ' of ' + recipientIds.length + ' recipient(s)');
        bootstrap.Modal.getInstance(document.getElementById('notifModal')).hide();
        renderTable();
    });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() { typeF = document.getElementById('typeFilter').value; statusF = document.getElementById('statusFilter').value; currentPage = 1; renderTable(); }

document.addEventListener('DOMContentLoaded', function () {
    populateRecipients();
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
