// Shared SweetAlert2 helpers so every delete/confirm flow across the app looks and behaves the
// same way, instead of each Feature script rolling its own native confirm()/toastr combo.

function confirmAction(message, options) {
    return Swal.fire(Object.assign({
        title: 'Are you sure?',
        text: message || '',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Yes, continue',
        cancelButtonText: 'Cancel',
        confirmButtonColor: '#0d9488',
        cancelButtonColor: '#64748b',
        reverseButtons: true,
    }, options || {}));
}

function confirmDelete(message) {
    return confirmAction(message || 'This action cannot be undone.', {
        title: 'Delete this?',
        icon: 'warning',
        confirmButtonText: 'Yes, delete it',
        confirmButtonColor: '#dc2626',
    });
}

function deletedAlert(message) {
    return Swal.fire({
        title: 'Deleted',
        text: message || 'The item has been deleted.',
        icon: 'success',
        confirmButtonColor: '#0d9488',
        timer: 1800,
        timerProgressBar: true,
    });
}
