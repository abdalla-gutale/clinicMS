function togglePw(inputId, btn) {
    var inp = document.getElementById(inputId);
    var icon = btn.querySelector('i');
    var show = inp.type === 'password';
    inp.type = show ? 'text' : 'password';
    icon.className = show ? 'ri-eye-line' : 'ri-eye-off-line';
}

function setBtnLoading(btn, loading) {
    btn.classList.toggle('loading', loading);
    btn.disabled = loading;
}

// ── Forgot Password page ──
function submitForgotPassword() {
    var identifier = document.getElementById('fIdentifier').value.trim();
    if (!identifier) { toastr.error('Enter your username or email'); return; }

    var btn = document.getElementById('submitBtn');
    setBtnLoading(btn, true);

    fetch('/Account/ForgotPassword', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ identifier: identifier })
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        setBtnLoading(btn, false);
        if (!result.ok) { toastr.error(result.data.message || 'Could not process request'); return; }
        document.getElementById('infoBox').classList.add('show');
        setTimeout(function () { window.location.href = '/Account/ResetPassword'; }, 1500);
    }).catch(function () {
        setBtnLoading(btn, false);
        toastr.error('Something went wrong. Please try again.');
    });
}

// ── Reset Password page ──
function submitResetPassword() {
    var code = document.getElementById('fCode').value.trim();
    var newPassword = document.getElementById('fNewPassword').value;
    var confirmPassword = document.getElementById('fConfirmPassword').value;

    if (!/^[0-9]{6}$/.test(code)) { toastr.error('Enter the 6-digit reset code'); return; }
    if (!newPassword || newPassword.length < 6) { toastr.error('Password must be at least 6 characters'); return; }
    if (newPassword !== confirmPassword) { toastr.error('Passwords do not match'); return; }

    var btn = document.getElementById('submitBtn');
    setBtnLoading(btn, true);

    fetch('/Account/ResetPassword', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code: code, newPassword: newPassword })
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        setBtnLoading(btn, false);
        if (!result.ok) { toastr.error(result.data.message || 'Could not reset password'); return; }
        toastr.success('Password reset. Please sign in.');
        setTimeout(function () { window.location.href = '/Account/Login'; }, 1200);
    }).catch(function () {
        setBtnLoading(btn, false);
        toastr.error('Something went wrong. Please try again.');
    });
}

function resendResetCode() {
    fetch('/Account/ResendResetCode', { method: 'POST' })
        .then(function (res) { return res.json().then(function (data) { return { ok: res.ok, data: data }; }); })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not resend code'); return; }
            toastr.success('A new code has been sent.');
        });
}
