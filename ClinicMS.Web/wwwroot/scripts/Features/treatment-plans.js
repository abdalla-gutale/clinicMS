var plans = (typeof TREATMENT_PLANS_DATA !== 'undefined' ? TREATMENT_PLANS_DATA : []);
var planServices = (typeof PLAN_SERVICES_DATA !== 'undefined' ? PLAN_SERVICES_DATA : []);
var planProducts = (typeof PLAN_PRODUCTS_DATA !== 'undefined' ? PLAN_PRODUCTS_DATA : []);
var planPatients = (typeof PLAN_PATIENTS_DATA !== 'undefined' ? PLAN_PATIENTS_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', editingId = null;
var currentSessions = []; // [{ sessionNumber, label, serviceIds:[int], productIds:[int] }]
var assignSessions = []; // [{ sessionNumber, label, serviceIds:[int], productIds:[int] }]

// ── List table ──
function filtered() {
    var q = searchQuery.toLowerCase();
    return plans.filter(function (p) { return !q || p.planName.toLowerCase().includes(q); });
}

function pricingLabel(p) { return p.pricingModelType === 'FixedPackage' ? 'Fixed Package' : 'Per Visit'; }
function priceLabel(p) { return p.pricingModelType === 'FixedPackage' ? (p.fixedPackagePrice || 0).toLocaleString() + ' AED' : 'Per Visit'; }

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('plansTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (p, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${p.planName}</span></td>
            <td><span class="gp-badge gp-badge-purple">${pricingLabel(p)}</span></td>
            <td style="font-weight:700;color:#0d9488;">${priceLabel(p)}</td>
            <td>${p.frequency}</td>
            <td>${p.totalSessions}</td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-assign" onclick="openAssignModal(${p.id})" title="Assign Patient"><i class="ri-user-add-line"></i></button>
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${p.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deletePlan(${p.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="7" class="text-center py-4 text-muted">No treatment plans found</td></tr>';
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

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }

// ── Price input guard: digits + one decimal point only ──
function isPriceKeyAllowed(e) {
    if (e.ctrlKey || e.metaKey || e.altKey) return true;
    var ch = e.key;
    if (ch.length !== 1) return true;
    if (ch >= '0' && ch <= '9') return true;
    if (ch === '.' && e.target.value.indexOf('.') === -1) return true;
    e.preventDefault();
    return false;
}

function sanitizePrice(input) {
    var value = input.value.replace(/[^0-9.]/g, '');
    var firstDot = value.indexOf('.');
    if (firstDot !== -1) {
        value = value.slice(0, firstDot + 1) + value.slice(firstDot + 1).replace(/\./g, '');
    }
    input.value = value;
}

// ── Total sessions input guard: digits only, max 3 digits ──
function isSessionsKeyAllowed(e) {
    if (e.ctrlKey || e.metaKey || e.altKey) return true;
    var ch = e.key;
    if (ch.length !== 1) return true;
    if (ch >= '0' && ch <= '9' && e.target.value.length < 3) return true;
    e.preventDefault();
    return false;
}

function onTotalSessionsInput(input) {
    input.value = input.value.replace(/[^0-9]/g, '').slice(0, 3);
    regenerateSessions();
}

// ── Pricing model toggle ──
function onPricingModelChange() {
    var isFixed = document.getElementById('fPricingModel').value === 'FixedPackage';
    document.getElementById('fixedPriceWrap').style.display = isFixed ? '' : 'none';
    if (!isFixed) document.getElementById('fFixedPrice').value = '';
}

// ── Sessions grid ──
function frequencyLabel(frequency, n) {
    if (frequency === 'Daily') return 'Day ' + n;
    if (frequency === 'Monthly') return 'Month ' + n;
    return 'Week ' + n;
}

function renderSessionRowHtml(index, session) {
    var serviceOptions = planServices.map(function (s) {
        return '<option value="' + s.id + '"' + (session.serviceIds.indexOf(s.id) !== -1 ? ' selected' : '') + '>' + s.serviceName + '</option>';
    }).join('');
    var productOptions = planProducts.map(function (p) {
        return '<option value="' + p.id + '"' + (session.productIds.indexOf(p.id) !== -1 ? ' selected' : '') + '>' + p.name + ' (' + p.price.toFixed(2) + ')</option>';
    }).join('');
    return '<tr id="sessionRow_' + index + '">' +
        '<td><span class="sessions-label" id="sessionLabel_' + index + '">' + session.label + '</span></td>' +
        '<td><select class="session-select" id="sessionService_' + index + '" multiple>' + serviceOptions + '</select></td>' +
        '<td><select class="session-select" id="sessionProduct_' + index + '" multiple>' + productOptions + '</select></td>' +
        '</tr>';
}

function initSessionRowSelect2(index) {
    if (typeof $ === 'undefined' || !$.fn || !$.fn.select2) return;
    $('#sessionService_' + index).select2({
        placeholder: 'Select services…', width: '100%', dropdownParent: $('#planModal'), theme: 'default'
    }).on('change', function () { currentSessions[index].serviceIds = ($(this).val() || []).map(Number); });
    $('#sessionProduct_' + index).select2({
        placeholder: 'Select products…', width: '100%', dropdownParent: $('#planModal'), theme: 'default'
    }).on('change', function () { currentSessions[index].productIds = ($(this).val() || []).map(Number); });
}

function destroySessionRowSelect2(index) {
    if (typeof $ === 'undefined' || !$.fn || !$.fn.select2) return;
    var svc = $('#sessionService_' + index), prod = $('#sessionProduct_' + index);
    if (svc.length && svc.hasClass('select2-hidden-accessible')) svc.select2('destroy');
    if (prod.length && prod.hasClass('select2-hidden-accessible')) prod.select2('destroy');
}

// Adds/removes rows at the tail to match Total Sessions, preserving existing row selections by
// position, and refreshes every row's label to match the current Frequency.
function regenerateSessions() {
    var totalStr = document.getElementById('fTotalSessions').value.trim();
    var total = parseInt(totalStr, 10);
    var frequency = document.getElementById('fFrequency').value;
    var tbody = document.getElementById('sessionsTableBody');
    var emptyMsg = document.getElementById('sessionsEmpty');

    if (!totalStr || isNaN(total) || total <= 0) {
        for (var d = 0; d < currentSessions.length; d++) destroySessionRowSelect2(d);
        currentSessions = [];
        tbody.innerHTML = '';
        emptyMsg.style.display = '';
        return;
    }
    emptyMsg.style.display = 'none';

    if (currentSessions.length > total) {
        for (var r = currentSessions.length - 1; r >= total; r--) {
            destroySessionRowSelect2(r);
            var rowEl = document.getElementById('sessionRow_' + r);
            if (rowEl) rowEl.remove();
        }
        currentSessions = currentSessions.slice(0, total);
    } else if (currentSessions.length < total) {
        for (var i = currentSessions.length; i < total; i++) {
            var session = { sessionNumber: i + 1, label: frequencyLabel(frequency, i + 1), serviceIds: [], productIds: [] };
            currentSessions.push(session);
            tbody.insertAdjacentHTML('beforeend', renderSessionRowHtml(i, session));
            initSessionRowSelect2(i);
        }
    }

    for (var j = 0; j < currentSessions.length; j++) {
        currentSessions[j].sessionNumber = j + 1;
        currentSessions[j].label = frequencyLabel(frequency, j + 1);
        var labelEl = document.getElementById('sessionLabel_' + j);
        if (labelEl) labelEl.textContent = currentSessions[j].label;
    }
}

// ── Add/Edit modal ──
function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Treatment Plan' : 'Add Treatment Plan';

    for (var d = 0; d < currentSessions.length; d++) destroySessionRowSelect2(d);
    document.getElementById('sessionsTableBody').innerHTML = '';
    currentSessions = [];

    if (id) {
        var p = plans.find(function (x) { return x.id === id; });
        document.getElementById('fPlanName').value = p.planName;
        document.getElementById('fPricingModel').value = p.pricingModelType;
        document.getElementById('fFixedPrice').value = p.fixedPackagePrice || '';
        document.getElementById('fFrequency').value = p.frequency;
        document.getElementById('fTotalSessions').value = p.totalSessions;
        onPricingModelChange();

        var tbody = document.getElementById('sessionsTableBody');
        currentSessions = p.sessions.map(function (s) {
            return { sessionNumber: s.sessionNumber, label: s.label, serviceIds: s.serviceIds.slice(), productIds: s.productIds.slice() };
        });
        currentSessions.forEach(function (session, index) {
            tbody.insertAdjacentHTML('beforeend', renderSessionRowHtml(index, session));
        });
        currentSessions.forEach(function (session, index) { initSessionRowSelect2(index); });
        document.getElementById('sessionsEmpty').style.display = currentSessions.length ? 'none' : '';
    } else {
        document.getElementById('fPlanName').value = '';
        document.getElementById('fPricingModel').value = 'FixedPackage';
        document.getElementById('fFixedPrice').value = '';
        document.getElementById('fFrequency').value = 'Weekly';
        document.getElementById('fTotalSessions').value = '';
        onPricingModelChange();
        document.getElementById('sessionsEmpty').style.display = '';
    }

    new bootstrap.Modal(document.getElementById('planModal')).show();
}

function savePlan() {
    var planName = document.getElementById('fPlanName').value.trim();
    var pricingModel = document.getElementById('fPricingModel').value;
    var fixedPrice = parseFloat(document.getElementById('fFixedPrice').value);
    var frequency = document.getElementById('fFrequency').value;
    var totalSessions = parseInt(document.getElementById('fTotalSessions').value, 10);

    if (!planName) { toastr.error('Plan name is required'); return; }
    if (pricingModel === 'FixedPackage' && (isNaN(fixedPrice) || fixedPrice <= 0)) { toastr.error('Fixed package price is required'); return; }
    if (isNaN(totalSessions) || totalSessions <= 0) { toastr.error('Total sessions is required'); return; }
    if (currentSessions.length !== totalSessions) { toastr.error('Session schedule does not match total sessions'); return; }

    var emptySession = currentSessions.find(function (s) { return s.serviceIds.length === 0 && s.productIds.length === 0; });
    if (emptySession) { toastr.error('"' + emptySession.label + '" needs at least one service or product'); return; }

    var body = {
        planName: planName,
        pricingModelType: pricingModel,
        fixedPackagePrice: pricingModel === 'FixedPackage' ? fixedPrice : null,
        frequency: frequency,
        totalSessions: totalSessions,
        sessions: currentSessions.map(function (s) {
            return { sessionNumber: s.sessionNumber, label: s.label, serviceIds: s.serviceIds, productIds: s.productIds };
        })
    };

    var url = editingId ? '/MedicalServices/UpdateTreatmentPlan?id=' + editingId : '/MedicalServices/CreateTreatmentPlan';
    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save treatment plan'); return; }
        if (editingId) {
            var idx = plans.findIndex(function (p) { return p.id === editingId; });
            if (idx >= 0) plans[idx] = result.data;
            toastr.success('Treatment plan updated');
        } else {
            plans.push(result.data);
            toastr.success('Treatment plan added');
        }
        bootstrap.Modal.getInstance(document.getElementById('planModal')).hide();
        renderTable();
    });
}

// ── Assign patient (inline, no page navigation) ──
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

function openAssignModal(planId) {
    var plan = plans.find(function (p) { return p.id === planId; });
    if (!plan) return;

    var patientSelect = document.getElementById('aPatient');
    patientSelect.innerHTML = '<option value="">Select a patient…</option>' + planPatients.map(function (p) {
        return '<option value="' + p.id + '">' + p.fullName + '</option>';
    }).join('');

    var planSelect = document.getElementById('aTreatmentPlan');
    planSelect.innerHTML = '<option value="' + plan.id + '">' + plan.planName + ' (' + pricingLabel(plan) + ')</option>';

    document.getElementById('aStartDate').min = todayIso();
    document.getElementById('aStartDate').value = todayIso();

    new bootstrap.Modal(document.getElementById('assignModal')).show();

    setTimeout(function () { regenerateAssignSessions(planId); }, 60);
}

function onAssignStartDateChange() {
    var planId = parseInt(document.getElementById('aTreatmentPlan').value, 10);
    refreshAssignSessionDates(planId);
}

function renderAssignSessionRowHtml(index, session, editable, dateText) {
    var serviceOptions = planServices.map(function (s) {
        return '<option value="' + s.id + '"' + (session.serviceIds.indexOf(s.id) !== -1 ? ' selected' : '') + '>' + s.serviceName + '</option>';
    }).join('');
    var productOptions = planProducts.map(function (p) {
        return '<option value="' + p.id + '"' + (session.productIds.indexOf(p.id) !== -1 ? ' selected' : '') + '>' + p.name + ' (' + p.price.toFixed(2) + ')</option>';
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
    var plan = plans.find(function (p) { return p.id === planId; });
    if (!plan) return;

    for (var d = 0; d < assignSessions.length; d++) destroyAssignSessionRowSelect2(d);

    var editable = plan.pricingModelType === 'PerVisit';
    document.getElementById('assignLockBanner').style.display = editable ? 'none' : '';

    var startIso = document.getElementById('aStartDate').value || todayIso();

    assignSessions = plan.sessions.map(function (s) {
        return { sessionNumber: s.sessionNumber, label: s.label, serviceIds: s.serviceIds.slice(), productIds: s.productIds.slice() };
    });

    var tbody = document.getElementById('assignSessionsTableBody');
    tbody.innerHTML = assignSessions.map(function (session, index) {
        var dateText = formatDateDisplay(computeSessionDate(startIso, plan.frequency, index));
        return renderAssignSessionRowHtml(index, session, editable, dateText);
    }).join('');

    assignSessions.forEach(function (session, index) { initAssignSessionRowSelect2(index, editable); });
}

function refreshAssignSessionDates(planId) {
    var plan = plans.find(function (p) { return p.id === planId; });
    if (!plan) return;
    var startIso = document.getElementById('aStartDate').value || todayIso();
    assignSessions.forEach(function (session, index) {
        var el = document.getElementById('assignSessionDate_' + index);
        if (el) el.textContent = formatDateDisplay(computeSessionDate(startIso, plan.frequency, index));
    });
}

function submitAssign() {
    var patientId = parseInt(document.getElementById('aPatient').value, 10);
    var treatmentPlanId = parseInt(document.getElementById('aTreatmentPlan').value, 10);
    var startDate = document.getElementById('aStartDate').value;

    if (isNaN(patientId)) { toastr.error('Please select a patient'); return; }
    if (isNaN(treatmentPlanId)) { toastr.error('Please select a treatment plan'); return; }
    if (!startDate) { toastr.error('Please choose a start date'); return; }
    if (startDate < todayIso()) { toastr.error('Start date cannot be in the past'); return; }

    var plan = plans.find(function (p) { return p.id === treatmentPlanId; });
    if (plan && plan.pricingModelType === 'PerVisit') {
        var emptySession = assignSessions.find(function (s) { return s.serviceIds.length === 0 && s.productIds.length === 0; });
        if (emptySession) { toastr.error('"' + emptySession.label + '" needs at least one service or product'); return; }
    }

    var body = {
        patientId: patientId,
        treatmentPlanId: treatmentPlanId,
        startDate: startDate,
        sessions: assignSessions.map(function (s) {
            return { sessionNumber: s.sessionNumber, serviceIds: s.serviceIds, productIds: s.productIds };
        })
    };

    fetch('/MedicalServices/AssignPatientCycle', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not assign patient'); return; }
        toastr.success('Patient assigned to treatment plan');
        bootstrap.Modal.getInstance(document.getElementById('assignModal')).hide();
    });
}

function deletePlan(id) {
    if (!confirm('Delete this treatment plan?')) return;
    fetch('/MedicalServices/DeleteTreatmentPlan?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete treatment plan'); return; }
            plans = plans.filter(function (p) { return p.id !== id; });
            toastr.success('Deleted');
            renderTable();
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
