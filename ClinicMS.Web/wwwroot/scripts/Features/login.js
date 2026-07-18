function togglePassword() {
    var inp  = document.getElementById('loginPass');
    var icon = document.getElementById('pwToggleIcon');
    var show = inp.type === 'password';
    inp.type = show ? 'text' : 'password';
    icon.className = show ? 'ri-eye-line' : 'ri-eye-off-line';
}

function doLogin() {
    var userVal = document.getElementById('loginUser').value.trim();
    var passVal = document.getElementById('loginPass').value;
    var valid   = true;

    document.getElementById('loginUser').classList.remove('is-error');
    document.getElementById('loginPass').classList.remove('is-error');
    document.getElementById('errUser').classList.remove('show');
    document.getElementById('errPass').classList.remove('show');
    document.getElementById('errGeneral').style.display = 'none';

    if (!userVal) {
        document.getElementById('loginUser').classList.add('is-error');
        document.getElementById('errUser').classList.add('show');
        valid = false;
    }
    if (!passVal) {
        document.getElementById('loginPass').classList.add('is-error');
        document.getElementById('errPass').classList.add('show');
        valid = false;
    }
    if (!valid) return;

    var btn = document.getElementById('loginBtn');
    btn.classList.add('loading');
    btn.disabled = true;

    fetch('/Account/Login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: userVal, password: passVal })
    })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            btn.classList.remove('loading');
            btn.disabled = false;

            if (!result.ok) {
                document.getElementById('loginUser').classList.add('is-error');
                document.getElementById('loginPass').classList.add('is-error');
                document.getElementById('errGeneral').style.display = 'flex';
                return;
            }

            sessionStorage.setItem('loginMaskedEmail', result.data.maskedEmail);
            window.location.href = '/Account/Otp';
        })
        .catch(function () {
            btn.classList.remove('loading');
            btn.disabled = false;
            document.getElementById('errGeneral').style.display = 'flex';
        });
}

document.addEventListener('DOMContentLoaded', function () {
    ['loginUser', 'loginPass'].forEach(function (id) {
        document.getElementById(id).addEventListener('keydown', function (e) {
            if (e.key === 'Enter') doLogin();
        });
    });
});
