var wiPatients = (typeof WALKIN_PATIENTS_DATA !== 'undefined' ? WALKIN_PATIENTS_DATA : []);
var wiServices = (typeof WALKIN_SERVICES_DATA !== 'undefined' ? WALKIN_SERVICES_DATA : []);
var wiProducts = (typeof WALKIN_PRODUCTS_DATA !== 'undefined' ? WALKIN_PRODUCTS_DATA : []);
var wiAccounts = (typeof WALKIN_ACCOUNTS_DATA !== 'undefined' ? WALKIN_ACCOUNTS_DATA : []);
var wiVatPercentage = (typeof WALKIN_VAT_PERCENTAGE !== 'undefined' ? WALKIN_VAT_PERCENTAGE : 0);

function money(n) { return (n || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }

function populateSelects() {
    document.getElementById('wPatient').innerHTML = '<option value="">Select a patient…</option>' +
        wiPatients.map(function (p) { return '<option value="' + p.id + '">' + p.fullName + '</option>'; }).join('');

    document.getElementById('wServices').innerHTML = wiServices.map(function (s) {
        return '<option value="' + s.id + '">' + s.serviceName + ' (' + money(s.price) + ')</option>';
    }).join('');

    document.getElementById('wProducts').innerHTML = wiProducts.map(function (p) {
        return '<option value="' + p.id + '">' + p.name + ' (' + money(p.price) + ')</option>';
    }).join('');

    document.getElementById('wAccount').innerHTML = '<option value="">— None —</option>' +
        wiAccounts.map(function (a) { return '<option value="' + a.id + '">' + a.name + '</option>'; }).join('');

    document.getElementById('wiVatPct').textContent = wiVatPercentage;
}

function selectedServices() {
    return ($('#wServices').val() || []).map(Number).map(function (id) {
        return wiServices.find(function (s) { return s.id === id; });
    }).filter(Boolean);
}

function selectedProducts() {
    return ($('#wProducts').val() || []).map(Number).map(function (id) {
        return wiProducts.find(function (p) { return p.id === id; });
    }).filter(Boolean);
}

function renderItemsAndTotals() {
    var services = selectedServices();
    var products = selectedProducts();
    var items = services.map(function (s) { return { name: s.serviceName, type: 'Service', price: s.price }; })
        .concat(products.map(function (p) { return { name: p.name, type: 'Product', price: p.price }; }));

    var tbody = document.getElementById('wiItemsBody');
    var emptyMsg = document.getElementById('wiItemsEmpty');
    if (items.length === 0) {
        tbody.innerHTML = '';
        emptyMsg.style.display = '';
    } else {
        emptyMsg.style.display = 'none';
        tbody.innerHTML = items.map(function (it) {
            var badgeClass = it.type === 'Service' ? 'wi-type-service' : 'wi-type-product';
            return '<tr><td>' + it.name + '</td><td><span class="wi-type-badge ' + badgeClass + '">' + it.type + '</span></td>' +
                '<td style="text-align:right;">' + money(it.price) + '</td></tr>';
        }).join('');
    }

    var subtotal = items.reduce(function (sum, it) { return sum + it.price; }, 0);
    var discountRaw = parseFloat(document.getElementById('wDiscount').value);
    var discount = isNaN(discountRaw) ? 0 : discountRaw;
    if (discount > subtotal) discount = subtotal;

    var vat = Math.round((subtotal - discount) * (wiVatPercentage / 100) * 100) / 100;
    var net = subtotal - discount + vat;

    document.getElementById('wiSubtotal').textContent = money(subtotal);
    document.getElementById('wiVat').textContent = money(vat);
    document.getElementById('wiNetTotal').textContent = money(net);
}

function onWalkInInputsChange() { renderItemsAndTotals(); }

function submitWalkInSale() {
    var patientId = parseInt(document.getElementById('wPatient').value, 10);
    var serviceIds = ($('#wServices').val() || []).map(Number);
    var productIds = ($('#wProducts').val() || []).map(Number);
    var discountRaw = parseFloat(document.getElementById('wDiscount').value);
    var discount = isNaN(discountRaw) ? 0 : discountRaw;
    var method = document.getElementById('wMethod').value;
    var accountVal = document.getElementById('wAccount').value;
    var reference = document.getElementById('wReference').value.trim() || null;

    if (isNaN(patientId)) { toastr.error('Please select a patient'); return; }
    if (serviceIds.length === 0 && productIds.length === 0) { toastr.error('Select at least one service or product'); return; }

    var body = {
        patientId: patientId,
        serviceIds: serviceIds,
        productIds: productIds,
        discountAmount: discount,
        paymentMethod: method,
        accountId: accountVal ? parseInt(accountVal, 10) : null,
        referenceNumber: reference
    };

    fetch('/MedicalServices/CreateWalkInSale', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    }).then(function (res) {
        return res.json().then(function (data) { return { ok: res.ok, data: data }; });
    }).then(function (result) {
        if (!result.ok) { toastr.error(result.data.message || 'Could not complete sale'); return; }
        toastr.success('Sale completed');
        showReceipt(result.data);
        resetForm();
    });
}

function showReceipt(invoice) {
    var receipt = document.getElementById('wiReceipt');
    receipt.style.display = '';
    receipt.innerHTML = '<i class="ri-checkbox-circle-line me-1"></i> Invoice <b>' + invoice.invoiceNumber + '</b> created for ' +
        '<b>' + money(invoice.netAmount) + ' ' + WALKIN_CURRENCY + '</b> — fully paid.';
}

function resetForm() {
    document.getElementById('wPatient').value = '';
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('#wPatient').trigger('change');
        $('#wServices').val(null).trigger('change');
        $('#wProducts').val(null).trigger('change');
        $('#wAccount').val('').trigger('change');
    } else {
        document.getElementById('wServices').selectedIndex = -1;
        document.getElementById('wProducts').selectedIndex = -1;
        document.getElementById('wAccount').value = '';
    }
    document.getElementById('wDiscount').value = '';
    document.getElementById('wReference').value = '';
    document.getElementById('wMethod').value = 'Cash';
    renderItemsAndTotals();
}

document.addEventListener('DOMContentLoaded', function () {
    populateSelects();
    initSelect2();
    renderItemsAndTotals();

    $('#wServices, #wProducts').on('change', renderItemsAndTotals);
});

function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('#wPatient, #wAccount, #wMethod').select2({ theme: 'default', width: '100%', dropdownParent: document.body });
        $('#wServices').select2({ theme: 'default', width: '100%', placeholder: 'Select services…', dropdownParent: document.body });
        $('#wProducts').select2({ theme: 'default', width: '100%', placeholder: 'Select products…', dropdownParent: document.body });
    }
}
