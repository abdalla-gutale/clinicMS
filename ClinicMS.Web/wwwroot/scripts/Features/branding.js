// Applies real clinic branding (name + logo) to the auth pages' brand marks.
// Falls back to the static "ClinicMS" defaults already in the markup when no
// clinic settings have been configured yet.
function applyClinicBranding() {
    var branding = (typeof CLINIC_BRANDING !== 'undefined') ? CLINIC_BRANDING : null;
    if (!branding) return;

    var name = branding.clinicName || 'ClinicMS';
    document.querySelectorAll('#heroBrandName, #formBrandName, #footerBrandName').forEach(function (el) {
        el.textContent = name;
    });

    if (branding.logoUrl) {
        document.querySelectorAll('#heroLogoWrap, #formLogoWrap').forEach(function (el) {
            var fallback = el.innerHTML;
            var img = document.createElement('img');
            img.src = branding.logoUrl;
            img.alt = name;
            img.style.cssText = 'width:100%;height:100%;object-fit:contain;';
            img.onerror = function () { el.innerHTML = fallback; };
            el.innerHTML = '';
            el.appendChild(img);
        });
    }
}

document.addEventListener('DOMContentLoaded', applyClinicBranding);
