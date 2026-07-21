// Every Feature script builds table rows by interpolating data straight into an innerHTML
// template literal -- none of it was escaped, so a patient name, note, or template message
// containing "<script>" or an onerror= payload would execute for whoever views that list. Wrap
// any user-supplied free-text field with this before interpolating it into HTML.
function escapeHtml(value) {
    if (value === null || value === undefined) return '';
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
