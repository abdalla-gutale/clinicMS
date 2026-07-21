var initialPage = (typeof PATIENTS_PAGE_DATA !== 'undefined' ? PATIENTS_PAGE_DATA : { items: [], page: 1, pageSize: 8, totalCount: 0 });

// The full patient list is no longer loaded client-side -- each page of rows is fetched from the
// server on demand, so currentPageItems only ever holds what's actually rendered right now (which
// is all openModal/deletePatient need to look up a row by id).
var currentPage = initialPage.page, pageSize = initialPage.pageSize, searchQuery = '', genderF = '', editingId = null;
var currentPageItems = initialPage.items, currentTotalCount = initialPage.totalCount;
var currentImageUrl = null;
var searchDebounceHandle = null;

var genderColors = { Male: 'blue', Female: 'pink', Other: 'gray' };

function fetchPage() {
    var params = new URLSearchParams({ page: currentPage, pageSize: pageSize });
    if (searchQuery) params.set('search', searchQuery);
    if (genderF) params.set('gender', genderF);

    return fetch('/Patients/GetPage?' + params.toString())
        .then(function (res) { return res.json(); })
        .then(function (result) {
            currentPageItems = result.items;
            currentTotalCount = result.totalCount;
            renderTable();
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
    if (p.imageUrl) return '<img class="gp-avatar" src="' + escapeHtml(p.imageUrl) + '" alt="' + escapeHtml(p.fullName) + '">';
    return '<span class="gp-avatar"><i class="ri-user-line"></i></span>';
}

function renderTable() {
    var pages = Math.ceil(currentTotalCount / pageSize) || 1;
    var tbody = document.getElementById('patientsTableBody');
    tbody.innerHTML = currentPageItems.length ? currentPageItems.map(function (p, i) { return `
        <tr>
            <td>${(currentPage - 1) * pageSize + i + 1}</td>
            <td>${avatarMarkup(p)}</td>
            <td><span style="font-weight:700;color:#1e293b;">${escapeHtml(p.fullName)}</span></td>
            <td><span class="gp-badge gp-badge-${genderColors[p.gender] || 'gray'}">${p.gender}</span></td>
            <td>${p.dateOfBirth}</td>
            <td>${calculateAge(p.dateOfBirth)}</td>
            <td>${escapeHtml(p.phone)}</td>
            <td>${p.email ? escapeHtml(p.email) : '<span class="text-muted">—</span>'}</td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${p.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deletePatient(${p.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="9" class="text-center py-4 text-muted">No patients found</td></tr>';
    document.getElementById('pageInfo').textContent = `Showing ${currentPageItems.length} of ${currentTotalCount}`;
    var btns = document.getElementById('pageBtns');
    btns.innerHTML = '';
    for (var pnum = 1; pnum <= pages; pnum++) {
        var btn = document.createElement('button');
        btn.className = 'gp-page-btn' + (pnum === currentPage ? ' active' : '');
        btn.textContent = pnum;
        btn.onclick = (function (pp) { return function () { currentPage = pp; fetchPage(); }; })(pnum);
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

// ── Photo preview + real upload ──
// Shows the local file immediately for feedback, then uploads it in the background so
// currentImageUrl holds a real server-side URL by the time the form is saved.
function previewPhoto(input) {
    if (!input.files || !input.files[0]) return;
    var file = input.files[0];

    var reader = new FileReader();
    reader.onload = function (e) {
        document.getElementById('fImagePreview').src = e.target.result;
        document.getElementById('fImagePreview').style.display = 'block';
        document.getElementById('fImageIcon').style.display = 'none';
    };
    reader.readAsDataURL(file);

    var formData = new FormData();
    formData.append('file', file);
    currentImageUrl = null;

    fetch('/Patients/UploadPhoto', { method: 'POST', body: formData })
        .then(function (res) { return res.json().then(function (data) { return { ok: res.ok, data: data }; }); })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not upload photo'); return; }
            currentImageUrl = result.data.url;
        })
        .catch(function () { toastr.error('Could not upload photo'); });
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
    currentImageUrl = null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Patient' : 'Add Patient';
    document.getElementById('fImagePreview').style.display = 'none';
    document.getElementById('fImagePreview').src = '';
    document.getElementById('fImageIcon').style.display = '';
    document.getElementById('fImageInput').value = '';

    if (id) {
        var p = currentPageItems.find(function (x) { return x.id === id; });
        document.getElementById('fFullName').value = p.fullName;
        document.getElementById('fGender').value = p.gender;
        document.getElementById('fAge').value = calculateAge(p.dateOfBirth);
        document.getElementById('fPhone').value = p.phone;
        document.getElementById('fEmail').value = p.email || '';
        if (p.imageUrl) {
            currentImageUrl = p.imageUrl;
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

    if (!fullName) { toastr.error('Full name is required'); return; }
    if (!age) { toastr.error('Age is required'); return; }
    if (!phone) { toastr.error('Phone is required'); return; }
    if (email && !EMAIL_PATTERN.test(email)) { toastr.error('Enter a valid email address'); return; }

    var body = { imageUrl: currentImageUrl, fullName: fullName, gender: gender, dateOfBirth: dobFromAge(age), phone: phone, email: email || null };

    var url = editingId ? '/Patients/Update?id=' + editingId : '/Patients/Create';
    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save patient'); return; }
        toastr.success(editingId ? 'Patient updated successfully' : 'Patient added successfully');
        bootstrap.Modal.getInstance(document.getElementById('patientModal')).hide();
        if (!editingId) currentPage = 1;
        fetchPage();
    });
}

function deletePatient(id) {
    confirmDelete('This patient will be permanently deleted.').then(function (confirmResult) {
        if (!confirmResult.isConfirmed) return;
        fetch('/Patients/Delete?id=' + id, { method: 'POST' })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, data: data }; });
            })
            .then(function (result) {
                if (!result.ok) { toastr.error(result.data.message || 'Could not delete patient'); return; }
                deletedAlert('Patient deleted.');
                // Deleting the last row on a page beyond the first would otherwise leave that page
                // empty even though earlier pages still have rows -- step back one page instead.
                if (currentPageItems.length === 1 && currentPage > 1) currentPage--;
                fetchPage();
            });
    });
}

function handleSearch(v) {
    searchQuery = v;
    currentPage = 1;
    clearTimeout(searchDebounceHandle);
    searchDebounceHandle = setTimeout(fetchPage, 300);
}
function handleFilter() {
    genderF = document.getElementById('genderFilter').value;
    currentPage = 1;
    fetchPage();
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
