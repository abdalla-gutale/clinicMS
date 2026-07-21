var cycles = (typeof PATIENT_CYCLES_DATA !== 'undefined' ? PATIENT_CYCLES_DATA : []);
var cyclePatients = (typeof CYCLE_PATIENTS_DATA !== 'undefined' ? CYCLE_PATIENTS_DATA : []);
var cycleTreatmentPlans = (typeof CYCLE_TREATMENT_PLANS_DATA !== 'undefined' ? CYCLE_TREATMENT_PLANS_DATA : []);
var cycleServices = (typeof CYCLE_SERVICES_DATA !== 'undefined' ? CYCLE_SERVICES_DATA : []);
var cycleProducts = (typeof CYCLE_PRODUCTS_DATA !== 'undefined' ? CYCLE_PRODUCTS_DATA : []);
var cyclePaymentAccounts = (typeof CYCLE_PAYMENT_ACCOUNTS_DATA !== 'undefined' ? CYCLE_PAYMENT_ACCOUNTS_DATA : []);
var discounts = (typeof CYCLE_DISCOUNTS_DATA !== 'undefined' ? CYCLE_DISCOUNTS_DATA : []);

function accountOptionsHtml() {
    return '<option value="">— None —</option>' + cyclePaymentAccounts.map(function (a) {
        return '<option value="' + a.id + '">' + a.name + '</option>';
    }).join('');
}

var currentPage = 1, perPage = 8, searchQuery = '';
var activeCycleId = null;
var activeSessions = []; // [{ sessionNumber, label, scheduledDate, status, serviceIds:[int], productIds:[int], chargeAmount, paidAmount }]

// ── Shared helpers ──
function todayIso() {
    var d = new Date();
    var m = ('0' + (d.getMonth() + 1)).slice(-2);
    var day = ('0' + d.getDate()).slice(-2);
    return d.getFullYear() + '-' + m + '-' + day;
}

function formatDateDisplay(iso) {
    var d = new Date(iso + 'T00:00:00');
    if (isNaN(d.getTime())) return '';
    return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function computeSessionDate(startIso, frequency, sessionIndex) {
    var d = new Date(startIso + 'T00:00:00');
    if (frequency === 'Daily') d.setDate(d.getDate() + sessionIndex);
    else if (frequency === 'Monthly') d.setMonth(d.getMonth() + sessionIndex);
    else d.setDate(d.getDate() + sessionIndex * 7);
    var m = ('0' + (d.getMonth() + 1)).slice(-2);
    var day = ('0' + d.getDate()).slice(-2);
    return d.getFullYear() + '-' + m + '-' + day;
}

function money(n) { return (n || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }

// ── List table ──
function filtered() {
    var q = searchQuery.toLowerCase();
    return cycles.filter(function (c) {
        return !q || c.patientName.toLowerCase().includes(q) || c.planName.toLowerCase().includes(q);
    });
}

function pricingLabel(c) { return c.pricingModelType === 'FixedPackage' ? 'Fixed Package' : 'Per Visit'; }

function cycleStatusBadge(status) {
    if (status === 'Paused') return '<span class="gp-badge gp-badge-red">Paused</span>';
    if (status === 'Completed') return '<span class="gp-badge gp-badge-blue">Completed</span>';
    return '<span class="gp-badge gp-badge-green">Active</span>';
}

function formatDate(iso) {
    var d = new Date(iso);
    if (isNaN(d.getTime())) return '';
    return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function renderPausedAlert() {
    var paused = cycles.filter(function (c) { return c.status === 'Paused'; });
    var el = document.getElementById('pausedAlert');
    if (!paused.length) { el.innerHTML = ''; return; }
    el.innerHTML =
        '<div class="paused-alert"><i class="ri-alarm-warning-line"></i><div>' +
        '<div><b>' + paused.length + (paused.length > 1 ? ' cycles are' : ' cycle is') + ' paused</b> — a scheduled session passed without being completed or rescheduled.</div>' +
        '<div class="paused-list">' + paused.map(function (c) { return escapeHtml(c.patientName) + ' (' + escapeHtml(c.planName) + ')'; }).join(' &middot; ') + '</div>' +
        '</div></div>';
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('cyclesTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (c, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(c.patientName)}</span></td>
            <td>${escapeHtml(c.planName)}</td>
            <td><span class="gp-badge gp-badge-purple">${pricingLabel(c)}</span></td>
            <td>${cycleStatusBadge(c.status)}</td>
            <td>${c.frequency}</td>
            <td>${c.totalSessions}</td>
            <td>${formatDate(c.assignedAt)}</td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openSessionsModal(${c.id})" title="Sessions"><i class="ri-calendar-check-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteCycle(${c.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="9" class="text-center py-4 text-muted">No patient cycles found</td></tr>';
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
    renderPausedAlert();
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

function applyCycleUpdate(updatedCycle) {
    var idx = cycles.findIndex(function (c) { return c.id === updatedCycle.id; });
    if (idx >= 0) cycles[idx] = updatedCycle; else cycles.push(updatedCycle);
    if (activeCycleId === updatedCycle.id) {
        renderSessionsModalBody(updatedCycle);
    }
    renderTable();
}

// ── Assign modal ──
var assignSessions = []; // [{ sessionNumber, label, serviceIds:[int], productIds:[int] }]

function openAssignModal() {
    var patientSelect = document.getElementById('fPatient');
    patientSelect.innerHTML = '<option value="">Select a patient…</option>' + cyclePatients.map(function (p) {
        return '<option value="' + p.id + '">' + escapeHtml(p.fullName) + '</option>';
    }).join('');

    var planSelect = document.getElementById('fTreatmentPlan');
    planSelect.innerHTML = '<option value="">Select a treatment plan…</option>' + cycleTreatmentPlans.map(function (p) {
        return '<option value="' + p.id + '">' + escapeHtml(p.planName) + '</option>';
    }).join('');

    document.getElementById('fStartDate').min = todayIso();
    document.getElementById('fStartDate').value = todayIso();

    document.getElementById('fPaymentModeWrap').style.display = 'none';
    document.getElementById('fDepositAmountWrap').style.display = 'none';
    document.getElementById('fDepositMethodWrap').style.display = 'none';
    document.getElementById('fDepositAccountWrap').style.display = 'none';
    document.getElementById('fDepositAccount').innerHTML = accountOptionsHtml();
    document.getElementById('fPaymentMode').value = 'PerSession';
    document.getElementById('fDepositAmount').value = '';

    assignSessions = [];
    document.getElementById('assignSessionsTableBody').innerHTML = '';
    document.getElementById('assignSessionsEmpty').style.display = '';
    document.getElementById('assignLockBanner').style.display = 'none';

    new bootstrap.Modal(document.getElementById('assignModal')).show();
}

function onAssignPlanChange() {
    var planId = parseInt(document.getElementById('fTreatmentPlan').value, 10);
    var plan = cycleTreatmentPlans.find(function (p) { return p.id === planId; });

    var isFixed = !!plan && plan.pricingModelType === 'FixedPackage';
    document.getElementById('fPaymentModeWrap').style.display = isFixed ? '' : 'none';
    if (isFixed) {
        document.getElementById('fPaymentMode').value = 'PerSession';
        onAssignPaymentModeChange();
    } else {
        document.getElementById('fDepositAmountWrap').style.display = 'none';
        document.getElementById('fDepositMethodWrap').style.display = 'none';
        document.getElementById('fDepositAccountWrap').style.display = 'none';
    }

    regenerateAssignSessions(planId);
}

function onAssignPaymentModeChange() {
    var isDeposit = document.getElementById('fPaymentMode').value === 'DepositBalance';
    document.getElementById('fDepositAmountWrap').style.display = isDeposit ? '' : 'none';
    document.getElementById('fDepositMethodWrap').style.display = isDeposit ? '' : 'none';
    document.getElementById('fDepositAccountWrap').style.display = isDeposit ? '' : 'none';
    if (!isDeposit) document.getElementById('fDepositAmount').value = '';
}

function onAssignStartDateChange() {
    var planId = parseInt(document.getElementById('fTreatmentPlan').value, 10);
    refreshAssignSessionDates(planId);
}

function renderAssignSessionRowHtml(index, session, editable, dateText) {
    var serviceOptions = cycleServices.map(function (s) {
        return '<option value="' + s.id + '"' + (session.serviceIds.indexOf(s.id) !== -1 ? ' selected' : '') + '>' + escapeHtml(s.serviceName) + '</option>';
    }).join('');
    var productOptions = cycleProducts.map(function (p) {
        return '<option value="' + p.id + '"' + (session.productIds.indexOf(p.id) !== -1 ? ' selected' : '') + '>' + escapeHtml(p.name) + ' (' + money(p.price) + ')</option>';
    }).join('');
    var disabledAttr = editable ? '' : ' disabled';
    return '<tr id="assignSessionRow_' + index + '">' +
        '<td><span class="sessions-label">' + session.label + '</span></td>' +
        '<td><select class="session-select" id="assignSessionService_' + index + '" multiple' + disabledAttr + '>' + serviceOptions + '</select></td>' +
        '<td><select class="session-select" id="assignSessionProduct_' + index + '" multiple' + disabledAttr + '>' + productOptions + '</select></td>' +
        '<td><span class="sessions-label" id="assignSessionDate_' + index + '">' + dateText + '</span></td>' +
        '</tr>';
}

function initAssignSessionRowSelect2(index, editable) {
    if (typeof $ === 'undefined' || !$.fn || !$.fn.select2) return;
    $('#assignSessionService_' + index).select2({
        placeholder: 'Select services…', width: '100%', dropdownParent: $('#assignModal'), theme: 'default'
    }).on('change', function () { assignSessions[index].serviceIds = ($(this).val() || []).map(Number); });
    $('#assignSessionProduct_' + index).select2({
        placeholder: 'Select products…', width: '100%', dropdownParent: $('#assignModal'), theme: 'default'
    }).on('change', function () { assignSessions[index].productIds = ($(this).val() || []).map(Number); });
    if (!editable) {
        $('#assignSessionService_' + index).prop('disabled', true).trigger('change.select2');
        $('#assignSessionProduct_' + index).prop('disabled', true).trigger('change.select2');
    }
}

function destroyAssignSessionRowSelect2(index) {
    if (typeof $ === 'undefined' || !$.fn || !$.fn.select2) return;
    var svc = $('#assignSessionService_' + index), prod = $('#assignSessionProduct_' + index);
    if (svc.length && svc.hasClass('select2-hidden-accessible')) svc.select2('destroy');
    if (prod.length && prod.hasClass('select2-hidden-accessible')) prod.select2('destroy');
}

function regenerateAssignSessions(planId) {
    var plan = cycleTreatmentPlans.find(function (p) { return p.id === planId; });
    var emptyMsg = document.getElementById('assignSessionsEmpty');
    var tbody = document.getElementById('assignSessionsTableBody');

    for (var d = 0; d < assignSessions.length; d++) destroyAssignSessionRowSelect2(d);

    if (!plan) {
        assignSessions = [];
        tbody.innerHTML = '';
        emptyMsg.style.display = '';
        document.getElementById('assignLockBanner').style.display = 'none';
        return;
    }
    emptyMsg.style.display = 'none';

    var editable = plan.pricingModelType === 'PerVisit';
    document.getElementById('assignLockBanner').style.display = editable ? 'none' : '';

    var startIso = document.getElementById('fStartDate').value || todayIso();

    assignSessions = plan.sessions.map(function (s) {
        return { sessionNumber: s.sessionNumber, label: s.label, serviceIds: s.serviceIds.slice(), productIds: s.productIds.slice() };
    });

    tbody.innerHTML = assignSessions.map(function (session, index) {
        var dateText = formatDateDisplay(computeSessionDate(startIso, plan.frequency, index));
        return renderAssignSessionRowHtml(index, session, editable, dateText);
    }).join('');

    assignSessions.forEach(function (session, index) { initAssignSessionRowSelect2(index, editable); });
}

function refreshAssignSessionDates(planId) {
    var plan = cycleTreatmentPlans.find(function (p) { return p.id === planId; });
    if (!plan) return;
    var startIso = document.getElementById('fStartDate').value || todayIso();
    assignSessions.forEach(function (session, index) {
        var el = document.getElementById('assignSessionDate_' + index);
        if (el) el.textContent = formatDateDisplay(computeSessionDate(startIso, plan.frequency, index));
    });
}

function assignCycle() {
    var patientId = parseInt(document.getElementById('fPatient').value, 10);
    var treatmentPlanId = parseInt(document.getElementById('fTreatmentPlan').value, 10);
    var startDate = document.getElementById('fStartDate').value;

    if (isNaN(patientId)) { toastr.error('Please select a patient'); return; }
    if (isNaN(treatmentPlanId)) { toastr.error('Please select a treatment plan'); return; }
    if (!startDate) { toastr.error('Please choose a start date'); return; }
    if (startDate < todayIso()) { toastr.error('Start date cannot be in the past'); return; }

    var plan = cycleTreatmentPlans.find(function (p) { return p.id === treatmentPlanId; });

    var body = {
        patientId: patientId,
        treatmentPlanId: treatmentPlanId,
        startDate: startDate,
        sessions: assignSessions.map(function (s) {
            return { sessionNumber: s.sessionNumber, serviceIds: s.serviceIds, productIds: s.productIds };
        }),
        paymentMode: null,
        depositAmount: null,
        depositPaymentMethod: null,
        depositAccountId: null
    };

    if (plan && plan.pricingModelType === 'PerVisit') {
        var emptySession = assignSessions.find(function (s) { return s.serviceIds.length === 0 && s.productIds.length === 0; });
        if (emptySession) { toastr.error('"' + emptySession.label + '" needs at least one service or product'); return; }
    }

    if (plan && plan.pricingModelType === 'FixedPackage') {
        body.paymentMode = document.getElementById('fPaymentMode').value;
        if (body.paymentMode === 'DepositBalance') {
            var depositRaw = document.getElementById('fDepositAmount').value.trim();
            if (depositRaw) {
                var deposit = parseFloat(depositRaw);
                if (isNaN(deposit) || deposit < 0) { toastr.error('Enter a valid deposit amount'); return; }
                body.depositAmount = deposit;
                body.depositPaymentMethod = document.getElementById('fDepositMethod').value;
                var depositAccountVal = document.getElementById('fDepositAccount').value;
                body.depositAccountId = depositAccountVal ? parseInt(depositAccountVal, 10) : null;
            }
        }
    }

    fetch('/MedicalServices/AssignPatientCycle', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not assign patient'); return; }
        cycles.push(result.data);
        toastr.success('Patient assigned to treatment plan');
        bootstrap.Modal.getInstance(document.getElementById('assignModal')).hide();
        renderTable();
    });
}

function deleteCycle(id) {
    confirmDelete('This patient cycle will be permanently removed.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/MedicalServices/DeletePatientCycle?id=' + id, { method: 'POST' })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not remove patient cycle'); return; }
                cycles = cycles.filter(function (c) { return c.id !== id; });
                deletedAlert('Patient cycle removed.');
                renderTable();
            });
    });
}

// ── Sessions modal (post-assignment: status, reschedule, complete + payment) ──
function sessionStatusBadge(session) {
    if (session.status === 'Completed') {
        return '<span class="gp-badge gp-badge-green">Completed</span>' +
            (session.paidAmount ? '<div class="sessions-sub">' + money(session.paidAmount) + ' AED paid</div>' : '');
    }
    var overdue = session.scheduledDate < todayIso();
    var badge = session.status === 'Rescheduled'
        ? '<span class="gp-badge gp-badge-amber">Rescheduled</span>'
        : '<span class="gp-badge gp-badge-grey">Upcoming</span>';
    if (overdue) badge += '<div class="sessions-sub" style="color:#dc2626;font-weight:700;">Overdue</div>';
    return badge;
}

function renderCycleSessionRowHtml(index, session, editable) {
    var serviceOptions = cycleServices.map(function (s) {
        return '<option value="' + s.id + '"' + (session.serviceIds.indexOf(s.id) !== -1 ? ' selected' : '') + '>' + escapeHtml(s.serviceName) + '</option>';
    }).join('');
    var productOptions = cycleProducts.map(function (p) {
        return '<option value="' + p.id + '"' + (session.productIds.indexOf(p.id) !== -1 ? ' selected' : '') + '>' + escapeHtml(p.name) + ' (' + p.price.toFixed(2) + ')</option>';
    }).join('');
    var completed = session.status === 'Completed';
    var disabledAttr = (editable && !completed) ? '' : ' disabled';
    var actionDisabled = completed ? ' disabled' : '';

    return '<tr id="cycleSessionRow_' + index + '">' +
        '<td><span class="sessions-label">' + session.label + '</span></td>' +
        '<td>' + sessionStatusBadge(session) + '</td>' +
        '<td>' + formatDateDisplay(session.scheduledDate) + '</td>' +
        '<td><select class="session-select" id="cycleSessionService_' + index + '" multiple' + disabledAttr + '>' + serviceOptions + '</select></td>' +
        '<td><select class="session-select" id="cycleSessionProduct_' + index + '" multiple' + disabledAttr + '>' + productOptions + '</select></td>' +
        '<td>' +
            '<button class="session-action-btn" onclick="openRescheduleModal(' + index + ')"' + actionDisabled + '>Reschedule</button>' +
            '<button class="session-action-btn primary" onclick="openCompleteModal(' + index + ')"' + actionDisabled + '>Complete</button>' +
        '</td>' +
        '</tr>';
}

function initCycleSessionRowSelect2(index, editable) {
    if (typeof $ === 'undefined' || !$.fn || !$.fn.select2) return;
    $('#cycleSessionService_' + index).select2({
        placeholder: 'Select services…', width: '100%', dropdownParent: $('#sessionsModal'), theme: 'default'
    }).on('change', function () { activeSessions[index].serviceIds = ($(this).val() || []).map(Number); });
    $('#cycleSessionProduct_' + index).select2({
        placeholder: 'Select products…', width: '100%', dropdownParent: $('#sessionsModal'), theme: 'default'
    }).on('change', function () { activeSessions[index].productIds = ($(this).val() || []).map(Number); });
    if (!editable) {
        $('#cycleSessionService_' + index).prop('disabled', true).trigger('change.select2');
        $('#cycleSessionProduct_' + index).prop('disabled', true).trigger('change.select2');
    }
}

function destroyCycleSessionRowSelect2(index) {
    if (typeof $ === 'undefined' || !$.fn || !$.fn.select2) return;
    var svc = $('#cycleSessionService_' + index), prod = $('#cycleSessionProduct_' + index);
    if (svc.length && svc.hasClass('select2-hidden-accessible')) svc.select2('destroy');
    if (prod.length && prod.hasClass('select2-hidden-accessible')) prod.select2('destroy');
}

function renderCyclePaymentPanel(cycle) {
    var panel = document.getElementById('cyclePaymentPanel');

    if (cycle.pricingModelType === 'PerVisit') {
        var collected = cycle.sessions.reduce(function (sum, s) { return sum + (s.paidAmount || 0); }, 0);
        panel.style.display = '';
        panel.innerHTML = '<div class="cycle-payment-stat"><span>Total Collected</span><b>' + money(collected) + ' AED</b></div>';
        return;
    }

    panel.style.display = '';
    var modeLabel = cycle.paymentMode === 'DepositBalance' ? 'Deposit + Balance' : 'Per Session';
    var html =
        '<div class="cycle-payment-stat"><span>Payment Mode</span><b>' + modeLabel + '</b></div>' +
        '<div class="cycle-payment-stat"><span>Total Price</span><b>' + money(cycle.totalPrice) + ' AED</b></div>' +
        '<div class="cycle-payment-stat"><span>Paid</span><b>' + money(cycle.paidAmount) + ' AED</b></div>' +
        '<div class="cycle-payment-stat"><span>Balance Due</span><b>' + money(cycle.balanceDue) + ' AED</b></div>';

    if (cycle.paymentMode === 'DepositBalance' && (cycle.balanceDue || 0) > 0) {
        html += '<button class="session-action-btn primary" onclick="toggleRecordPaymentForm()">Record Payment</button>';
    }
    html += '<div id="recordPaymentFormWrap" style="width:100%;"></div>';
    panel.innerHTML = html;
}

function toggleRecordPaymentForm() {
    var wrap = document.getElementById('recordPaymentFormWrap');
    if (wrap.innerHTML) { wrap.innerHTML = ''; return; }
    wrap.innerHTML =
        '<div class="session-expand-form" style="margin-top:10px;">' +
        '<div class="fld"><label class="required">Amount</label><input type="text" inputmode="decimal" id="recordPaymentAmount" placeholder="0.00"></div>' +
        '<div class="fld"><label>Method</label><select id="recordPaymentMethod">' +
        '<option value="Cash">Cash</option><option value="CreditCard">Credit Card</option><option value="BankTransfer">Bank Transfer</option><option value="WalletCredit">Wallet Credit</option>' +
        '</select></div>' +
        '<div class="fld"><label>Account</label><select id="recordPaymentAccount">' + accountOptionsHtml() + '</select></div>' +
        '<div class="fld"><label>Reference (optional)</label><input type="text" id="recordPaymentReference"></div>' +
        '<button class="session-action-btn primary" onclick="submitRecordPayment()">Confirm Payment</button>' +
        '<button class="session-action-btn" onclick="document.getElementById(\'recordPaymentFormWrap\').innerHTML=\'\'">Cancel</button>' +
        '</div>';
}

function submitRecordPayment() {
    var amount = parseFloat(document.getElementById('recordPaymentAmount').value);
    var method = document.getElementById('recordPaymentMethod').value;
    var accountVal = document.getElementById('recordPaymentAccount').value;
    var reference = document.getElementById('recordPaymentReference').value.trim() || null;

    if (isNaN(amount) || amount <= 0) { toastr.error('Enter a valid payment amount'); return; }

    fetch('/MedicalServices/RecordCyclePayment?id=' + activeCycleId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ amount: amount, paymentMethod: method, referenceNumber: reference, accountId: accountVal ? parseInt(accountVal, 10) : null })
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not record payment'); return; }
        applyCycleUpdate(result.data);
        toastr.success('Payment recorded');
    });
}

// ── Reschedule modal ──
var rescheduleSessionIndex = -1;
var rescheduleNextSessionDate = null;

function addDaysIso(iso, days) {
    var d = new Date(iso + 'T00:00:00');
    d.setDate(d.getDate() + days);
    var m = ('0' + (d.getMonth() + 1)).slice(-2);
    var day = ('0' + d.getDate()).slice(-2);
    return d.getFullYear() + '-' + m + '-' + day;
}

function updateRescheduleDateBounds() {
    var scope = document.querySelector('input[name="rescheduleScope"]:checked').value;
    var dateInput = document.getElementById('rescheduleDate');
    var hint = document.getElementById('rescheduleDateHint');

    if (scope === 'cascade' || !rescheduleNextSessionDate) {
        dateInput.removeAttribute('max');
        hint.textContent = 'From ' + formatDateDisplay(dateInput.min) + ' onward.';
    } else {
        var maxDate = addDaysIso(rescheduleNextSessionDate, -1);
        dateInput.max = maxDate;
        if (dateInput.value > maxDate) dateInput.value = maxDate;
        hint.textContent = 'Between ' + formatDateDisplay(dateInput.min) + ' and ' + formatDateDisplay(maxDate) + '.';
    }
}

function openRescheduleModal(index) {
    rescheduleSessionIndex = index;
    var session = activeSessions[index];

    var prevSession = null, nextSession = null;
    for (var i = 0; i < activeSessions.length; i++) {
        if (activeSessions[i].sessionNumber < session.sessionNumber) prevSession = activeSessions[i];
        if (activeSessions[i].sessionNumber > session.sessionNumber && !nextSession) nextSession = activeSessions[i];
    }

    var minDate = todayIso();
    if (prevSession && prevSession.scheduledDate > minDate) minDate = prevSession.scheduledDate;
    rescheduleNextSessionDate = nextSession ? nextSession.scheduledDate : null;

    document.getElementById('rescheduleModalTitle').textContent = 'Reschedule ' + session.label;
    document.getElementById('rescheduleCascadeWrap').style.display = nextSession ? '' : 'none';
    document.querySelector('input[name="rescheduleScope"][value="one"]').checked = true;

    var dateInput = document.getElementById('rescheduleDate');
    dateInput.min = minDate;
    dateInput.value = session.scheduledDate < minDate ? minDate : session.scheduledDate;
    updateRescheduleDateBounds();

    bootstrap.Modal.getInstance(document.getElementById('sessionsModal')).hide();
    new bootstrap.Modal(document.getElementById('rescheduleModal')).show();
}

function closeRescheduleModal() {
    var instance = bootstrap.Modal.getInstance(document.getElementById('rescheduleModal'));
    if (instance) instance.hide();
    new bootstrap.Modal(document.getElementById('sessionsModal')).show();
}

function submitReschedule() {
    var session = activeSessions[rescheduleSessionIndex];
    var newDate = document.getElementById('rescheduleDate').value;
    var scopeInput = document.querySelector('input[name="rescheduleScope"]:checked');
    var scope = scopeInput ? scopeInput.value : 'one';

    if (!newDate) { toastr.error('Please choose a date'); return; }
    if (newDate < todayIso()) { toastr.error('Cannot reschedule to a past date'); return; }

    fetch('/MedicalServices/ReschedulePatientCycleSession?id=' + activeCycleId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sessionNumber: session.sessionNumber, newDate: newDate, cascadeToFollowing: scope === 'cascade' })
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not reschedule session'); return; }
        applyCycleUpdate(result.data);
        toastr.success('Session rescheduled');
        var instance = bootstrap.Modal.getInstance(document.getElementById('rescheduleModal'));
        if (instance) instance.hide();
        new bootstrap.Modal(document.getElementById('sessionsModal')).show();
    });
}

// ── Complete modal (invoice: total, discount, VAT, net, paid, balance) ──
function sessionCharge(session, cycle) {
    if (cycle.pricingModelType === 'PerVisit') {
        var servicesTotal = session.serviceIds.reduce(function (sum, id) {
            var svc = cycleServices.find(function (s) { return s.id === id; });
            return sum + (svc ? svc.price : 0);
        }, 0);
        var productsTotal = session.productIds.reduce(function (sum, id) {
            var prod = cycleProducts.find(function (p) { return p.id === id; });
            return sum + (prod ? prod.price : 0);
        }, 0);
        return servicesTotal + productsTotal;
    }
    if (cycle.paymentMode === 'PerSession') {
        var total = cycle.totalPrice || 0;
        var perSession = cycle.totalSessions > 0 ? Math.round((total / cycle.totalSessions) * 100) / 100 : 0;
        var completedCount = cycle.sessions.filter(function (s) { return s.status === 'Completed'; }).length;
        var isLast = completedCount === cycle.totalSessions - 1;
        return isLast ? (total - perSession * (cycle.totalSessions - 1)) : perSession;
    }
    return null; // Deposit + Balance mode -- no per-session charge
}

// A configured discount applies if its type is Both, or ServiceOnly/ProductOnly and the session
// actually has that kind of item. When several qualify, the highest percentage wins.
function findConfiguredDiscount(hasServices, hasProducts) {
    var today = todayIso();
    var eligible = discounts.filter(function (d) {
        if (!d.isActive || d.startDate > today || d.endDate < today) return false;
        if (d.discountType === 'Both') return true;
        if (d.discountType === 'ServiceOnly') return hasServices;
        if (d.discountType === 'ProductOnly') return hasProducts;
        return false;
    });
    if (!eligible.length) return null;
    eligible.sort(function (a, b) { return b.discountValue - a.discountValue; });
    return eligible[0];
}

var completeSessionIndex = -1;
var completeTotalAmountValue = 0;
var completeConfiguredDiscount = null;

function isCompletePriceKeyAllowed(e) {
    if (e.ctrlKey || e.metaKey || e.altKey) return true;
    var ch = e.key;
    if (ch.length !== 1) return true;
    if (ch >= '0' && ch <= '9') return true;
    if (ch === '.' && e.target.value.indexOf('.') === -1) return true;
    e.preventDefault();
    return false;
}

function sanitizeCompleteAmount(value, max) {
    value = value.replace(/[^0-9.]/g, '');
    var firstDot = value.indexOf('.');
    if (firstDot !== -1) value = value.slice(0, firstDot + 1) + value.slice(firstDot + 1).replace(/\./g, '');
    var num = parseFloat(value);
    if (max != null && !isNaN(num) && num > max) value = String(max);
    return value;
}

function renderCompleteLineItems(session) {
    var rows = [];
    session.serviceIds.forEach(function (id) {
        var svc = cycleServices.find(function (s) { return s.id === id; });
        if (svc) rows.push({ name: svc.serviceName, price: svc.price });
    });
    session.productIds.forEach(function (id) {
        var prod = cycleProducts.find(function (p) { return p.id === id; });
        if (prod) rows.push({ name: prod.name, price: prod.price });
    });
    document.getElementById('completeLineItems').innerHTML = rows.length ? rows.map(function (r) {
        return '<div class="invoice-line-item"><span>' + escapeHtml(r.name) + '</span><span>' + money(r.price) + '</span></div>';
    }).join('') : '<div class="invoice-line-item"><span class="text-muted">No items</span></div>';
}

function renderConfiguredDiscountHint() {
    var hint = document.getElementById('completeConfiguredDiscountHint');
    if (!completeConfiguredDiscount) { hint.innerHTML = ''; return; }
    var amount = Math.round(completeTotalAmountValue * completeConfiguredDiscount.discountValue) / 100;
    hint.innerHTML =
        '<div class="configured-discount-hint">' +
        '<span>Configured discount available: <b>' + escapeHtml(completeConfiguredDiscount.discountName) + '</b> (' + completeConfiguredDiscount.discountValue + '%)</span>' +
        '<button type="button" class="session-action-btn" onclick="applyConfiguredDiscount()">Apply ' + money(amount) + '</button>' +
        '</div>';
}

function applyConfiguredDiscount() {
    if (!completeConfiguredDiscount) return;
    var amount = Math.round(completeTotalAmountValue * completeConfiguredDiscount.discountValue) / 100;
    document.getElementById('completeDiscountAmount').value = amount.toFixed(2);
    recomputeCompleteTotals();
}

// The toggle being "open" is what enables the manual discount amount field for editing.
function onCompleteDiscountToggle() {
    var enabled = document.getElementById('completeDiscountToggle').checked;
    document.getElementById('completeDiscountInputWrap').style.display = enabled ? '' : 'none';
    document.getElementById('completeDiscountAmount').disabled = !enabled;
    if (!enabled) document.getElementById('completeDiscountAmount').value = '';
    recomputeCompleteTotals();
}

function onCompleteDiscountInput(input) {
    input.value = sanitizeCompleteAmount(input.value, completeTotalAmountValue);
    recomputeCompleteTotals();
}

function currentCompleteDiscount() {
    var enabled = document.getElementById('completeDiscountToggle').checked;
    if (!enabled) return 0;
    var value = parseFloat(document.getElementById('completeDiscountAmount').value);
    return isNaN(value) ? 0 : Math.min(value, completeTotalAmountValue);
}

function currentCompleteNet() {
    var total = completeTotalAmountValue || 0;
    var discount = currentCompleteDiscount();
    var vatPct = parseFloat(CYCLE_VAT_PERCENTAGE) || 0;
    var vat = Math.round((total - discount) * vatPct) / 100;
    return { discount: discount, vat: vat, net: total - discount + vat };
}

function recomputeCompleteTotals() {
    var totals = currentCompleteNet();
    document.getElementById('completeDiscountAmountDisplay').textContent = money(totals.discount);
    document.getElementById('completeVatAmount').textContent = money(totals.vat);
    document.getElementById('completeNetAmount').textContent = money(totals.net);

    var paidInput = document.getElementById('completePaidAmount');
    if (!paidInput.dataset.touched) paidInput.value = totals.net.toFixed(2);
    updateCompleteBalance();
}

function onCompletePaidInput(input) {
    input.dataset.touched = '1';
    input.value = sanitizeCompleteAmount(input.value, null);
    updateCompleteBalance();
}

function updateCompleteBalance() {
    var net = currentCompleteNet().net;
    var paid = parseFloat(document.getElementById('completePaidAmount').value) || 0;
    document.getElementById('completeBalanceAmount').value = money(net - paid);
}

function openCompleteModal(index) {
    completeSessionIndex = index;
    var cycle = cycles.find(function (c) { return c.id === activeCycleId; });
    var session = activeSessions[index];
    var total = sessionCharge(session, cycle);
    var hasCharge = total && total > 0;

    document.getElementById('completeModalTitle').textContent = 'Complete ' + session.label;
    document.getElementById('completeNoChargeNotice').style.display = hasCharge ? 'none' : '';
    document.getElementById('completeInvoiceWrap').style.display = hasCharge ? '' : 'none';

    if (hasCharge) {
        completeTotalAmountValue = total;
        completeConfiguredDiscount = findConfiguredDiscount(session.serviceIds.length > 0, session.productIds.length > 0);

        renderCompleteLineItems(session);
        document.getElementById('completeTotalAmount').textContent = money(total);

        document.getElementById('completeDiscountToggle').checked = false;
        document.getElementById('completeDiscountAmount').value = '';
        document.getElementById('completeDiscountAmount').disabled = true;
        document.getElementById('completeDiscountInputWrap').style.display = 'none';
        renderConfiguredDiscountHint();

        document.getElementById('completeMethod').value = 'Cash';
        document.getElementById('completeAccount').innerHTML = accountOptionsHtml();
        document.getElementById('completeReference').value = '';

        var paidInput = document.getElementById('completePaidAmount');
        paidInput.value = '';
        delete paidInput.dataset.touched;

        recomputeCompleteTotals();
    }

    bootstrap.Modal.getInstance(document.getElementById('sessionsModal')).hide();
    new bootstrap.Modal(document.getElementById('completeModal')).show();
}

function closeCompleteModal() {
    var instance = bootstrap.Modal.getInstance(document.getElementById('completeModal'));
    if (instance) instance.hide();
    new bootstrap.Modal(document.getElementById('sessionsModal')).show();
}

function submitComplete() {
    var session = activeSessions[completeSessionIndex];
    var hasCharge = document.getElementById('completeInvoiceWrap').style.display !== 'none';

    var body = { sessionNumber: session.sessionNumber, discountAmount: 0, paidAmount: 0, paymentMethod: null, referenceNumber: null, accountId: null };

    if (hasCharge) {
        var totals = currentCompleteNet();
        var paid = parseFloat(document.getElementById('completePaidAmount').value);

        if (isNaN(paid) || paid <= 0) { toastr.error('Enter a valid paid amount'); return; }
        if (paid > totals.net + 0.01) { toastr.error('Paid amount cannot exceed the net amount'); return; }

        body.discountAmount = totals.discount;
        body.paidAmount = paid;
        body.paymentMethod = document.getElementById('completeMethod').value;
        var accountVal = document.getElementById('completeAccount').value;
        body.accountId = accountVal ? parseInt(accountVal, 10) : null;
        body.referenceNumber = document.getElementById('completeReference').value.trim() || null;
    }

    fetch('/MedicalServices/CompletePatientCycleSession?id=' + activeCycleId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not complete session'); return; }
        applyCycleUpdate(result.data);
        toastr.success('Session marked completed');
        var instance = bootstrap.Modal.getInstance(document.getElementById('completeModal'));
        if (instance) instance.hide();
        new bootstrap.Modal(document.getElementById('sessionsModal')).show();
    });
}

function renderSessionsModalBody(cycle) {
    var editable = cycle.pricingModelType === 'PerVisit';

    document.getElementById('sessionsModalTitle').textContent = cycle.patientName + ' — ' + cycle.planName;
    document.getElementById('sessionsLockBanner').style.display = editable ? 'none' : '';
    document.getElementById('saveSessionsBtn').style.display = editable ? '' : 'none';

    for (var d = 0; d < activeSessions.length; d++) destroyCycleSessionRowSelect2(d);

    activeSessions = cycle.sessions.map(function (s) {
        return {
            sessionNumber: s.sessionNumber, label: s.label, scheduledDate: s.scheduledDate, status: s.status,
            serviceIds: s.serviceIds.slice(), productIds: s.productIds.slice(),
            chargeAmount: s.chargeAmount, paidAmount: s.paidAmount
        };
    });

    var tbody = document.getElementById('cycleSessionsTableBody');
    tbody.innerHTML = activeSessions.map(function (session, index) { return renderCycleSessionRowHtml(index, session, editable); }).join('');

    activeSessions.forEach(function (session, index) {
        if (session.status !== 'Completed') initCycleSessionRowSelect2(index, editable);
    });

    renderCyclePaymentPanel(cycle);
}

function openSessionsModal(id) {
    activeCycleId = id;
    var cycle = cycles.find(function (c) { return c.id === id; });
    if (!cycle) return;

    renderSessionsModalBody(cycle);

    new bootstrap.Modal(document.getElementById('sessionsModal')).show();
}

function saveSessions() {
    var cycle = cycles.find(function (c) { return c.id === activeCycleId; });
    if (!cycle || cycle.pricingModelType !== 'PerVisit') return;

    var editableSessions = activeSessions.filter(function (s) { return s.status !== 'Completed'; });
    var emptySession = editableSessions.find(function (s) { return s.serviceIds.length === 0 && s.productIds.length === 0; });
    if (emptySession) { toastr.error('"' + emptySession.label + '" needs at least one service or product'); return; }

    var body = {
        sessions: editableSessions.map(function (s) {
            return { sessionNumber: s.sessionNumber, serviceIds: s.serviceIds, productIds: s.productIds };
        })
    };

    fetch('/MedicalServices/UpdatePatientCycleSessions?id=' + activeCycleId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save sessions'); return; }
        applyCycleUpdate(result.data);
        toastr.success('Sessions updated');
    });
}

document.addEventListener('DOMContentLoaded', function () {
    renderTable();
});

// -- Select2 init (top-level form selects only; session row selects are init'd individually) --
function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select').select2({
            theme: 'default',
            width: '100%',
            dropdownParent: document.body
        });
    }
}
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
