var smsTemplates = (typeof SMS_TEMPLATES_DATA !== 'undefined' ? SMS_TEMPLATES_DATA : []);

var tplIcons = ['ri-gift-line', 'ri-calendar-check-line', 'ri-money-dollar-circle-line', 'ri-alarm-warning-line', 'ri-chat-smile-2-line'];

function renderTemplates() {
    var grid = document.getElementById('templateGrid');
    if (!smsTemplates.length) {
        grid.innerHTML = '<div class="text-muted" style="grid-column:1/-1;">No SMS templates configured yet.</div>';
        return;
    }
    grid.innerHTML = smsTemplates.map(function (t, i) { return `
        <div class="tpl-card" onclick="useTemplate(${t.id})">
            <div class="tpl-card-icon"><i class="${tplIcons[i % tplIcons.length]}" style="color:#0d9488;"></i></div>
            <div class="tpl-card-name">${t.templateTypeName} <span style="font-weight:600;color:#94a3b8;">(${t.channelType})</span></div>
            <div class="tpl-card-desc">${t.messageBody.length > 90 ? t.messageBody.slice(0, 90) + '…' : t.messageBody}</div>
            <button type="button" class="tpl-use-btn"><i class="ri-arrow-right-line"></i> Use Template</button>
        </div>`; }).join('');
}

function useTemplate(id) {
    var t = smsTemplates.find(function (x) { return x.id === id; });
    if (!t) return;
    document.getElementById('smsText').value = t.messageBody;
    onMessageInput();
    if (typeof toastr !== 'undefined') toastr.info('Template loaded into composer', 'Template');
}

// ── Recipient picker (no backend recipient source wired yet -- kept as UI-only placeholder) ──
function onRecipientChange() {
    var type = document.getElementById('recipientType').value;
    document.getElementById('individualWrap').style.display = type === 'individual' ? '' : 'none';
}
function onIndividualChange() {}

// ── Composer / preview ──
function onMessageInput() {
    var text = document.getElementById('smsText').value;
    var len = text.length;
    document.getElementById('charCount').textContent = len;
    var parts = len === 0 ? 1 : Math.ceil(len / 160);
    document.getElementById('smsParts').textContent = parts + ' SMS';
    document.getElementById('summaryParts').textContent = parts;

    var counterEl = document.getElementById('charCounter');
    counterEl.parentElement.classList.remove('warn', 'over');
    if (len > 160 && len <= 320) counterEl.parentElement.classList.add('warn');
    if (len > 320) counterEl.parentElement.classList.add('over');

    var bubble = document.getElementById('previewBubble');
    var timeEl = document.getElementById('previewTime');
    if (text.trim()) {
        bubble.textContent = text;
        bubble.classList.remove('empty');
        timeEl.style.display = '';
    } else {
        bubble.textContent = 'Your message will appear here…';
        bubble.classList.add('empty');
        timeEl.style.display = 'none';
    }
}

function clearCompose() {
    document.getElementById('smsText').value = '';
    onMessageInput();
}

function sendSms() {
    if (typeof toastr !== 'undefined') {
        toastr.warning('Sending isn\'t connected to a provider yet -- only templates and configuration are wired up so far.', 'Not available');
    }
}

document.addEventListener('DOMContentLoaded', function () {
    renderTemplates();
    onMessageInput();
});
