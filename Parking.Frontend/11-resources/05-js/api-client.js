const API_BASE_URL = window.API_BASE_URL || localStorage.getItem("apiBaseUrl") || "http://localhost:8080";
const AUTH_BASE_URL = window.AUTH_BASE_URL || localStorage.getItem("authBaseUrl") || "http://localhost:8081";

function getAuthToken() {
    return localStorage.getItem("authToken");
}

function setAuthToken(token) {
    if (token) {
        localStorage.setItem("authToken", token);
    } else {
        localStorage.removeItem("authToken");
    }
}

function setLoggedInUser(email) {
    if (email) {
        localStorage.setItem("loggedInUser", email);
    } else {
        localStorage.removeItem("loggedInUser");
    }
}

function getUserRole() {
    return localStorage.getItem("userRole");
}

function setUserRole(role) {
    if (role) {
        localStorage.setItem("userRole", role);
    } else {
        localStorage.removeItem("userRole");
    }
}

function decodeJwtPayload(token) {
    try {
        const base64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
        return JSON.parse(atob(base64));
    } catch {
        return null;
    }
}

function extractErrorMessage(payload, fallback) {
    if (!payload) {
        return fallback;
    }
    if (typeof payload === "string") {
        return payload;
    }
    if (payload.message) {
        return payload.message;
    }
    if (payload.error) {
        return payload.error;
    }
    if (Array.isArray(payload.errors) && payload.errors.length) {
        return payload.errors.join(" ");
    }
    return fallback;
}

async function apiFetchWithBase(baseUrl, path, options = {}) {
    const token = getAuthToken();
    const headers = Object.assign(
        { "Content-Type": "application/json" },
        options.headers || {}
    );

    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    let response;
    try {
        response = await fetch(`${baseUrl}${path}`, {
            ...options,
            headers
        });
    } catch (error) {
        throw new Error("Nieoczekiwany błąd, spróbuj później.");
    }

    const contentType = response.headers.get("content-type") || "";
    const hasJson = contentType.includes("application/json");
    let payload = null;

    if (hasJson) {
        payload = await response.json().catch(() => null);
    } else {
        const textPayload = await response.text().catch(() => "");
        payload = textPayload || null;
    }

    if (!response.ok) {
        const message = extractErrorMessage(payload, "Nieoczekiwany błąd, spróbuj później.");
        const error = new Error(message);
        error.status = response.status;
        error.payload = payload;
        if (!options.skipAuthRedirect && (response.status === 401 || response.status === 403)) {
            setAuthToken(null);
            setLoggedInUser(null);
            window.location.href = "00-01-login.html";
        }
        throw error;
    }

    return payload;
}

async function apiFetch(path, options = {}) {
    return apiFetchWithBase(API_BASE_URL, path, options);
}

async function apiFetchAuth(path, options = {}) {
    return apiFetchWithBase(AUTH_BASE_URL, path, { skipAuthRedirect: true, ...options });
}

function storeAuthFromResponse(payload) {
    if (!payload) {
        return;
    }
    const token = payload.token || payload.accessToken || payload.jwt || payload.bearer;
    if (token) {
        setAuthToken(token);
        const jwtPayload = decodeJwtPayload(token);
        if (jwtPayload) {
            const roleClaim = jwtPayload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
            const role = Array.isArray(roleClaim) ? roleClaim[0] : roleClaim;
            if (role) {
                setUserRole(role);
            }
        }
    }
    if (!getUserRole() && Array.isArray(payload.roles) && payload.roles.length) {
        setUserRole(payload.roles[0]);
    }
}
