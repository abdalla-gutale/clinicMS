function normalizePermission(value) {
    return String(value ?? '')
        .trim()
        .toLowerCase()
        .replace(/^permissions\./, '')
        .replace(/[^a-z0-9]/g, '');
}

export function getCurrentPermissions() {
    const perms = window.currentUserPermissions;
    return Array.isArray(perms) ? perms : [];
}

export function hasPermission(requiredPermission) {
    if (!requiredPermission) return true;

    const all = getCurrentPermissions();
    if (!all.length) return false;

    const normalizedOwned = new Set(all.map(normalizePermission));
    const required = Array.isArray(requiredPermission)
        ? requiredPermission
        : [requiredPermission];

    return required.some((p) => normalizedOwned.has(normalizePermission(p)));
}
