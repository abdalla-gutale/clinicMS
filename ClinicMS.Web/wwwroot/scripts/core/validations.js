export function setupValidation(formId) {
    const form = document.getElementById(formId);
    if (!form) return;

    const inputs = form.querySelectorAll('input[required], select[required], textarea[required]');

    inputs.forEach(input => {
        input.addEventListener('input', function () {
            validateField(this);
        });

        input.addEventListener('blur', function () {
            validateField(this);
        });
    });
}

export function validateField(field) {
    const $field = $(field);
    const value = field.value.trim();
    const type = field.type;
    const isSelect2 = $field.hasClass('select2-hidden-accessible');
    let isValid = true;

    // Basic required check
    if (field.hasAttribute('required') && !value) {
        isValid = false;
    }

    // Phone validation (if type is tel or has data-type="phone")
    if (isValid && value && (type === 'tel' || $field.data('type') === 'phone')) {
        const phoneRegex = /^[0-9+]{8,15}$/;
        if (!phoneRegex.test(value.replace(/\s/g, ''))) {
            isValid = false;
        }
    }

    // Numeric validation (min/max)
    if (isValid && value && type === 'number') {
        const num = Number(value);
        const min = field.getAttribute('min');
        const max = field.getAttribute('max');
        if (min !== null && num < Number(min)) isValid = false;
        if (max !== null && num > Number(max)) isValid = false;
    }

    if (!isValid) {
        field.classList.remove('is-valid');
        field.classList.add('is-invalid');
        if (isSelect2) {
            $field.next('.select2-container').find('.select2-selection').addClass('border-danger');
        }
        return false;
    } else {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
        if (isSelect2) {
            $field.next('.select2-container').find('.select2-selection').removeClass('border-danger');
        }
        return true;
    }
}

export function validateForm(formId) {
    const form = document.getElementById(formId);
    if (!form) return true;

    const inputs = form.querySelectorAll('input[required], select[required], textarea[required]');
    let firstInvalid = null;
    let isValid = true;

    inputs.forEach(input => {
        if (!validateField(input)) {
            isValid = false;
            if (!firstInvalid) firstInvalid = input;
        }
    });

    if (firstInvalid) {
        firstInvalid.focus();
    }

    return isValid;
}
