/**
 * Centralized Notification System for Portal Portal
 */
export const Toast = {
    /**
     * Show a success notification
     * @param {string} message 
     */
    success: (message) => {
        Toastify({
            text: message,
            duration: 3000,
            close: true,
            gravity: "top",
            position: "right",
            stopOnFocus: true,
            style: {
                background: "#00b09b",
                borderRadius: "10px",
                boxShadow: "0 4px 12px rgba(0,0,0,0.1)"
            }
        }).showToast();
    },

    /**
     * Show an error notification
     * @param {string} message 
     */
    error: (message) => {
        Toastify({
            text: message || "An error occurred",
            duration: 4000,
            close: true,
            gravity: "top",
            position: "right",
            stopOnFocus: true,
            style: {
                background: "#ff5f6d",
                borderRadius: "10px",
                boxShadow: "0 4px 12px rgba(0,0,0,0.1)"
            }
        }).showToast();
    },

    /**
     * Show an informational notification
     * @param {string} message 
     */
    info: (message) => {
        Toastify({
            text: message,
            duration: 3000,
            close: true,
            gravity: "top",
            position: "right",
            stopOnFocus: true,
            style: {
                background: "#ffc371",
                borderRadius: "10px",
                boxShadow: "0 4px 12px rgba(0,0,0,0.1)"
            }
        }).showToast();
    }
};
