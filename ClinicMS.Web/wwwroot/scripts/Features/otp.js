var boxes     = [];
var verifyBtn = null;
var attempts  = 0;
var timerInterval, resendInterval;
var resendSec = 60;
var verifying = false;

function autoSubmitIfComplete() {
    if (verifying) return;
    if (!boxes.every(function (b) { return b.value !== ''; })) return;
    verifying = true;
    verifyOtp();
}

document.addEventListener('DOMContentLoaded', function () {
    boxes     = Array.from(document.querySelectorAll('.otp-box'));
    verifyBtn = document.getElementById('verifyBtn');

    // Redirect if there's no pending login (user landed here directly / session expired)
    var maskedEmail = sessionStorage.getItem('loginMaskedEmail');
    if (!maskedEmail) { window.location.href = '/Account/Login'; return; }

    document.getElementById('otpEmailDisplay').textContent = maskedEmail;

    // Wire up boxes
    boxes.forEach(function (box, idx) {
        box.addEventListener('input', function () {
            this.value = this.value.replace(/[^0-9]/g, '').slice(-1);
            this.classList.toggle('filled', this.value !== '');
            if (this.value && idx < boxes.length - 1) boxes[idx + 1].focus();
            verifyBtn.disabled = !boxes.every(function (b) { return b.value !== ''; });
            autoSubmitIfComplete();
        });

        box.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && !this.value && idx > 0) {
                boxes[idx - 1].value = '';
                boxes[idx - 1].classList.remove('filled');
                boxes[idx - 1].focus();
            }
            if (e.key === 'ArrowLeft'  && idx > 0)               boxes[idx - 1].focus();
            if (e.key === 'ArrowRight' && idx < boxes.length - 1) boxes[idx + 1].focus();
        });

        box.addEventListener('paste', function (e) {
            e.preventDefault();
            var pasted = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '').slice(0, 6);
            pasted.split('').forEach(function (ch, i) {
                if (boxes[i]) { boxes[i].value = ch; boxes[i].classList.add('filled'); }
            });
            var next = Math.min(pasted.length, boxes.length - 1);
            boxes[next].focus();
            verifyBtn.disabled = !boxes.every(function (b) { return b.value !== ''; });
            autoSubmitIfComplete();
        });
    });

    boxes[0].focus();
    startTimer();
    startResendTimer();
});

// ── 5-minute expiry countdown ─────────────────────────────
function startTimer() {
    var totalSeconds = 300;
    var timerEl = document.getElementById('timerDisplay');
    timerInterval = setInterval(function () {
        totalSeconds--;
        if (totalSeconds <= 0) { clearInterval(timerInterval); timerEl.textContent = '00:00'; return; }
        var m = Math.floor(totalSeconds / 60);
        var s = totalSeconds % 60;
        timerEl.textContent = (m < 10 ? '0' : '') + m + ':' + (s < 10 ? '0' : '') + s;
        if (totalSeconds <= 30) timerEl.style.color = '#ef4444';
    }, 1000);
}

// ── 60-second resend countdown ────────────────────────────
function startResendTimer() {
    resendSec = 60;
    var resendLink = document.getElementById('resendLink');
    var resendEl   = document.getElementById('resendTimer');
    resendLink.classList.add('disabled');

    resendInterval = setInterval(function () {
        resendSec--;
        if (resendEl) resendEl.textContent = resendSec;
        if (resendSec <= 0) {
            clearInterval(resendInterval);
            resendLink.classList.remove('disabled');
            resendLink.innerHTML = 'Resend code';
        }
    }, 1000);
}

function resendOtp() {
    var resendLink = document.getElementById('resendLink');
    if (resendLink.classList.contains('disabled')) return;

    fetch('/Account/ResendOtp', { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) {
                if (typeof toastr !== 'undefined') toastr.error(result.data.message || 'Could not resend the code.', 'Error');
                return;
            }
            resendLink.innerHTML = 'Resend (<span id="resendTimer">60</span>s)';
            startResendTimer();
            if (typeof toastr !== 'undefined') toastr.info('A new OTP has been sent to your email', 'OTP Sent');
            boxes.forEach(function (b) { b.value = ''; b.classList.remove('filled', 'is-error'); });
            boxes[0].focus();
            verifyBtn.disabled = true;
            attempts = 0;
            verifying = false;
        });
}

// ── Verify ────────────────────────────────────────────────
function verifyOtp() {
    var code = boxes.map(function (b) { return b.value; }).join('');
    verifyBtn.classList.add('loading');
    verifyBtn.disabled = true;

    fetch('/Account/VerifyOtp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ otpCode: code })
    })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            verifyBtn.classList.remove('loading');

            if (result.ok) {
                sessionStorage.removeItem('loginMaskedEmail');
                clearInterval(timerInterval);
                window.location.href = '/Home/Index';
                return;
            }

            attempts++;
            boxes.forEach(function (b) { b.classList.add('is-error'); });
            setTimeout(function () {
                boxes.forEach(function (b) { b.classList.remove('is-error'); b.value = ''; b.classList.remove('filled'); });
                boxes[0].focus();
                verifyBtn.disabled = true;
                verifying = false;
            }, 600);

            var left = 3 - attempts;
            if (left <= 0) {
                if (typeof toastr !== 'undefined') toastr.error('Too many failed attempts. Redirecting to login…', 'Blocked');
                setTimeout(function () { window.location.href = '/Account/Login'; }, 2000);
            } else if (typeof toastr !== 'undefined') {
                toastr.warning(result.data.message || ('Incorrect code. ' + left + ' attempt' + (left > 1 ? 's' : '') + ' remaining.'), 'Wrong OTP');
            }
        })
        .catch(function () {
            verifyBtn.classList.remove('loading');
            if (typeof toastr !== 'undefined') toastr.error('Something went wrong. Please try again.', 'Error');
        });
}
