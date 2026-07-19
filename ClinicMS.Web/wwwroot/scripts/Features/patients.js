var patients = (typeof PATIENTS_DATA !== 'undefined' ? PATIENTS_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', genderF = '', editingId = null;

var genderColors = { Male: 'blue', Female: 'pink', Other: 'gray' };

function filtered() {
    return patients.filter(function (p) {
        var q = searchQuery.toLowerCase();
        var match = !q || p.fullName.toLowerCase().includes(q) || p.phone.toLowerCase().includes(q) || (p.email || '').toLowerCase().includes(q);
        var g = !genderF || p.gender === genderF;
        return match && g;
    });
}

function calculateAge(isoDob) {
    var dob = new Date(isoDob);
    var today = new Date();
    var age = today.getFullYear() - dob.getFullYear();
    var hasHadBirthdayThisYear = (today.getMonth() > dob.getMonth()) ||
        (today.getMonth() === dob.getMonth() && today.getDate() >= dob.getDate());
    if (!hasHadBirthdayThisYear) age--;
    return age;
}

function avatarMarkup(p) {
    if (p.imageUrl) return '<img class="gp-avatar" src="' + p.imageUrl + '" alt="' + p.fullName + '">';
    return '<span class="gp-avatar"><i class="ri-user-line"></i></span>';
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('patientsTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (p, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td>${avatarMarkup(p)}</td>
            <td><span style="font-weight:700;color:#1e293b;">${p.fullName}</span></td>
            <td><span class="gp-badge gp-badge-${genderColors[p.gender] || 'gray'}">${p.gender}</span></td>
            <td>${p.dateOfBirth}</td>
            <td>${calculateAge(p.dateOfBirth)}</td>
            <td>${p.phone}</td>
            <td>${p.email || '<span class="text-muted">—</span>'}</td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${p.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deletePatient(${p.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="9" class="text-center py-4 text-muted">No patients found</td></tr>';
    document.getElementById('pageInfo').textContent = `Showing ${slice.length} of ${total}`;
    var btns = document.getElementById('pageBtns');
    btns.innerHTML = '';
    for (var pnum = 1; pnum <= pages; pnum++) {
        var btn = document.createElement('button');
        btn.className = 'gp-page-btn' + (pnum === currentPage ? ' active' : '');
        btn.textContent = pnum;
        btn.onclick = (function (pp) { return function () { currentPage = pp; renderTable(); }; })(pnum);
        btns.appendChild(btn);
    }
}

// ── Age is the entered field; Date of Birth is derived from it (approximated using today's
// month/day so re-deriving the age from the computed DOB round-trips to the same number) ──
function dobFromAge(age) {
    var today = new Date();
    var year = today.getFullYear() - parseInt(age, 10);
    var dob = new Date(year, today.getMonth(), today.getDate());
    return dob.toISOString().slice(0, 10);
}

// ── Age input guard: digits only, max 3 digits ──
function isAgeKeyAllowed(e) {
    if (e.ctrlKey || e.metaKey || e.altKey) return true;
    var ch = e.key;
    if (ch.length !== 1) return true;
    if (ch >= '0' && ch <= '9' && e.target.value.length < 3) return true;
    e.preventDefault();
    return false;
}

function isAgePasteAllowed(e) {
    var text = (e.clipboardData || window.clipboardData).getData('text');
    if (!/^[0-9]{1,3}$/.test(text)) { e.preventDefault(); return false; }
    return true;
}

function sanitizeAge(input) {
    input.value = input.value.replace(/[^0-9]/g, '').slice(0, 3);
}

// ── Photo preview (local-only, no upload endpoint yet) ──
function previewPhoto(input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            document.getElementById('fImagePreview').src = e.target.result;
            document.getElementById('fImagePreview').style.display = 'block';
            document.getElementById('fImageIcon').style.display = 'none';
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// ── Phone: digits + leading "+" and common separators only, no letters ──
function isPhoneKeyAllowed(e) {
    if (e.ctrlKey || e.metaKey || e.altKey) return true;
    var ch = e.key;
    if (ch.length !== 1) return true;
    if (ch >= '0' && ch <= '9') return true;
    if ('+-() '.indexOf(ch) !== -1) return true;
    e.preventDefault();
    return false;
}

function isPhonePasteAllowed(e) {
    var text = (e.clipboardData || window.clipboardData).getData('text');
    if (!/^[0-9+\-() ]*$/.test(text)) { e.preventDefault(); return false; }
    return true;
}

function sanitizePhone(input) {
    input.value = input.value.replace(/[^0-9+\-() ]/g, '');
}

function sanitizeEmail(input) {
    input.value = input.value.replace(/\s/g, '');
}

var EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Patient' : 'Add Patient';
    document.getElementById('fImagePreview').style.display = 'none';
    document.getElementById('fImagePreview').src = '';
    document.getElementById('fImageIcon').style.display = '';
    document.getElementById('fImageInput').value = '';

    if (id) {
        var p = patients.find(function (x) { return x.id === id; });
        document.getElementById('fFullName').value = p.fullName;
        document.getElementById('fGender').value = p.gender;
        document.getElementById('fAge').value = calculateAge(p.dateOfBirth);
        document.getElementById('fPhone').value = p.phone;
        document.getElementById('fEmail').value = p.email || '';
        if (p.imageUrl) {
            document.getElementById('fImagePreview').src = p.imageUrl;
            document.getElementById('fImagePreview').style.display = 'block';
            document.getElementById('fImageIcon').style.display = 'none';
        }
    } else {
        document.getElementById('fFullName').value = '';
        document.getElementById('fGender').value = 'Male';
        document.getElementById('fAge').value = '';
        document.getElementById('fPhone').value = '';
        document.getElementById('fEmail').value = '';
    }
    new bootstrap.Modal(document.getElementById('patientModal')).show();
}

function savePatient() {
    var fullName = document.getElementById('fFullName').value.trim();
    var gender = document.getElementById('fGender').value;
    var age = document.getElementById('fAge').value.trim();
    var phone = document.getElementById('fPhone').value.trim();
    var email = document.getElementById('fEmail').value.trim();
    var imageUrl = document.getElementById('fImagePreview').style.display === 'none' ? null : document.getElementById('fImagePreview').src;

    if (!fullName) { toastr.error('Full name is required'); return; }
    if (!age) { toastr.error('Age is required'); return; }
    if (!phone) { toastr.error('Phone is required'); return; }
    if (email && !EMAIL_PATTERN.test(email)) { toastr.error('Enter a valid email address'); return; }

    var body = { imageUrl: imageUrl, fullName: fullName, gender: gender, dateOfBirth: dobFromAge(age), phone: phone, email: email || null };

    var url = editingId ? '/Patients/Update?id=' + editingId : '/Patients/Create';
    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save patient'); return; }
        if (editingId) {
            var idx = patients.findIndex(function (p) { return p.id === editingId; });
            if (idx >= 0) patients[idx] = result.data;
            toastr.success('Patient updated successfully');
        } else {
            patients.unshift(result.data);
            toastr.success('Patient added successfully');
        }
        bootstrap.Modal.getInstance(document.getElementById('patientModal')).hide();
        renderTable();
    });
}

function deletePatient(id) {
    if (!confirm('Delete this patient?')) return;
    fetch('/Patients/Delete?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete patient'); return; }
            patients = patients.filter(function (p) { return p.id !== id; });
            toastr.success('Patient deleted');
            renderTable();
        });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() {
    genderF = document.getElementById('genderFilter').value;
    currentPage = 1; renderTable();
}

document.addEventListener('DOMContentLoaded', function () {
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
document.addEventListener('show.bs.modal', function () { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
