import { Toast } from './notifications.js';

export async function httpRequest(baseUrl, endpoint, payload = {}, method = "POST", isFormData = false) {
    try {
        // Build URL reliably
        let url = baseUrl.endsWith('/') ? baseUrl : baseUrl + '/';
        if (endpoint) url += endpoint.startsWith('/') ? endpoint.substring(1) : endpoint;
        else if (url.endsWith('/')) url = url.slice(0, -1);

        const ajaxOptions = {
            url,
            type: method,
            processData: !isFormData,
            contentType: isFormData ? false : "application/json; charset=utf-8",
            dataType: "json",
            xhrFields: {
                withCredentials: true // Support session cookies
            }
        };

        if (method === "GET" || method === "DELETE") {
            if (payload && Object.keys(payload).length > 0) {
                const qs = new URLSearchParams(payload).toString();
                ajaxOptions.url += (url.includes("?") ? "&" : "?") + qs;
            }
        } else if (method === "POST" || method === "PUT") {
            ajaxOptions.data = isFormData ? payload : JSON.stringify(payload);
        }

        const response = await jQuery.ajax(ajaxOptions);

        const normalizeMessages = (rawMessages) => {
            if (!rawMessages) return [];
            if (Array.isArray(rawMessages)) return rawMessages.filter(Boolean).map(String);
            if (Array.isArray(rawMessages.$values)) return rawMessages.$values.filter(Boolean).map(String);
            if (typeof rawMessages === "string") return [rawMessages];
            return [];
        };

        // --- Robust Unwrapping ---
        let data = response;

        // Check for success property (case-insensitive)
        const isSuccess = response?.isSuccessful || response?.IsSuccessful || response?.success || response?.Success;
        const messages = normalizeMessages(response?.messages ?? response?.Messages);

        if (isSuccess) {
            data = [response.responseData, response.ResponseData, response.data, response.Data].find(v => v !== undefined) ?? response;
        } else {
            // If property exists but is false, or if there are error messages
            if (messages.length > 0) {
                messages.forEach(msg => Toast.error(msg));
            }
            return { data: [], messages, isSuccessful: false };
        }

        // Unwrap $values (System.Text.Json serialization artifact)
        if (data && typeof data === 'object' && data.$values) {
            data = data.$values;
        }

        // Final deep check if responseData was an object containing $values
        if (data && typeof data === 'object' && data.responseData?.$values) {
            data = data.responseData.$values;
        }

        return { data: data ?? [], messages: messages, isSuccessful: !!isSuccess };

    } catch (err) {
        console.error(`HTTP error at ${endpoint}:`, err);

        const normalizeMessages = (rawMessages) => {
            if (!rawMessages) return [];
            if (Array.isArray(rawMessages)) return rawMessages.filter(Boolean).map(String);
            if (Array.isArray(rawMessages.$values)) return rawMessages.$values.filter(Boolean).map(String);
            if (typeof rawMessages === "string") return [rawMessages];
            return [];
        };

        // Try to extract API messages from non-2xx responses (e.g., 400/404)
        const responseJson = err?.responseJSON
            || (() => {
                try {
                    return err?.responseText ? JSON.parse(err.responseText) : null;
                } catch {
                    return null;
                }
            })();

        const apiMessages = normalizeMessages(responseJson?.messages ?? responseJson?.Messages);
        if (apiMessages.length > 0) {
            apiMessages.forEach(msg => Toast.error(msg));
            return { data: [], messages: apiMessages, isSuccessful: false };
        }

        const msg = "A system error occurred. Please try again.";
        Toast.error(msg);
        return { data: [], messages: [msg], isSuccessful: false };
    }
}
