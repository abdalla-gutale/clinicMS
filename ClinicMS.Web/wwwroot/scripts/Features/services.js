var services = (typeof SERVICES_DATA !== 'undefined' ? SERVICES_DATA : []);
var serviceTypes = (typeof SERVICE_TYPES_DATA !== 'undefined' ? SERVICE_TYPES_DATA : []);

var currentPage = 1, perPage = 8, searchQuery = '', typeF = '', statusF = '', editingId = null;

function filtered() {
    return services.filter(function (s) {
        var q = searchQuery.toLowerCase();
        var match = !q || s.serviceName.toLowerCase().includes(q) || s.serviceTypeName.toLowerCase().includes(q);
        var t = !typeF || s.serviceTypeName === typeF;
        var st = !statusF || (s.isActive ? 'Active' : 'Inactive') === statusF;
        return match && t && st;
    });
}

function renderTable() {
    var data = filtered();
    var total = data.length;
    var pages = Math.ceil(total / perPage) || 1;
    if (currentPage > pages) currentPage = 1;
    var slice = data.slice((currentPage - 1) * perPage, currentPage * perPage);
    var tbody = document.getElementById('servicesTableBody');
    tbody.innerHTML = slice.length ? slice.map(function (s, i) { return `
        <tr>
            <td>${(currentPage - 1) * perPage + i + 1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${s.serviceName}</span></td>
            <td><span class="gp-badge gp-badge-teal">${s.serviceTypeName}</span></td>
            <td style="font-weight:700;color:#0d9488;">${s.price.toLocaleString()}</td>
            <td style="color:#64748b;">${s.description || ''}</td>
            <td><span class="gp-badge ${s.isActive ? 'gp-badge-green' : 'gp-badge-red'}">${s.isActive ? 'Active' : 'Inactive'}</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${s.id})" title="Edit"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteService(${s.id})" title="Delete"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`; }).join('') : '<tr><td colspan="7" class="text-center py-4 text-muted">No services found</td></tr>';
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

function populateTypeFilter() {
    var sel = document.getElementById('typeFilter');
    sel.innerHTML = '<option value="">All Types</option>' +
        serviceTypes.map(function (t) { return `<option>${t.typeName}</option>`; }).join('');
}

function populateTypeSelect(selectedId) {
    var sel = document.getElementById('fServiceType');
    sel.innerHTML = serviceTypes.map(function (t) {
        return `<option value="${t.id}" ${t.id === selectedId ? 'selected' : ''}>${t.typeName}</option>`;
    }).join('');
}

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

function openModal(id) {
    editingId = id || null;
    document.getElementById('modalTitle').textContent = id ? 'Edit Service' : 'Add Service';
    if (id) {
        var s = services.find(function (x) { return x.id === id; });
        populateTypeSelect(s.serviceTypeId);
        document.getElementById('fServiceName').value = s.serviceName;
        document.getElementById('fPrice').value = s.price;
        document.getElementById('fDescription').value = s.description || '';
        document.getElementById('fActive').checked = s.isActive;
    } else {
        populateTypeSelect(serviceTypes.length ? serviceTypes[0].id : null);
        document.getElementById('fServiceName').value = '';
        document.getElementById('fPrice').value = '';
        document.getElementById('fDescription').value = '';
        document.getElementById('fActive').checked = true;
    }
    new bootstrap.Modal(document.getElementById('serviceModal')).show();
}

function saveService() {
    var serviceTypeId = parseInt(document.getElementById('fServiceType').value, 10);
    var serviceName = document.getElementById('fServiceName').value.trim();
    var price = parseFloat(document.getElementById('fPrice').value);
    var description = document.getElementById('fDescription').value.trim();
    var isActive = document.getElementById('fActive').checked;

    if (!serviceTypeId) { toastr.error('Service type is required'); return; }
    if (!serviceName) { toastr.error('Service name is required'); return; }
    if (isNaN(price) || price <= 0) { toastr.error('Enter a valid service price'); return; }

    var body = { serviceTypeId: serviceTypeId, serviceName: serviceName, price: price, description: description || null, isActive: isActive };
    var url = editingId ? '/MedicalServices/UpdateService?id=' + editingId : '/MedicalServices/CreateService';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not save service'); return; }
        if (editingId) {
            var idx = services.findIndex(function (s) { return s.id === editingId; });
            if (idx >= 0) services[idx] = result.data;
            toastr.success('Service updated');
        } else {
            services.push(result.data);
            toastr.success('Service added');
        }
        bootstrap.Modal.getInstance(document.getElementById('serviceModal')).hide();
        renderTable();
    });
}

function deleteService(id) {
    if (!confirm('Delete this service?')) return;
    fetch('/MedicalServices/DeleteService?id=' + id, { method: 'POST' })
        .then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        })
        .then(function (result) {
            if (!result.ok) { toastr.error(result.data.message || 'Could not delete service'); return; }
            services = services.filter(function (s) { return s.id !== id; });
            toastr.success('Deleted');
            renderTable();
        });
}

function handleSearch(v) { searchQuery = v; currentPage = 1; renderTable(); }
function handleFilter() {
    typeF = document.getElementById('typeFilter').value;
    statusF = document.getElementById('statusFilter').value;
    currentPage = 1; renderTable();
}

document.addEventListener('DOMContentLoaded', function () {
    populateTypeFilter();
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
