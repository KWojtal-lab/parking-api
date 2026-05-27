document.getElementById("client-login-form").addEventListener("submit", async function(e) {
    e.preventDefault();

    const email = document.getElementById("client-login").value.trim();
    const password = document.getElementById("client-password").value;
    const errorEl = document.getElementById("client-login-error-msg");

    errorEl.innerText = "";

    if (!email || !password) {
        errorEl.innerText = "Podaj email i hasło.";
        return;
    }

    try {
        const payload = await apiFetchAuth("/api/auth/login", {
            method: "POST",
            body: JSON.stringify({ email, password })
        });

        storeAuthFromResponse(payload);
        setLoggedInUser(email);
        const role = getUserRole();
        window.location.href = role === "Operator"
            ? "02-00-operator-dashboard.html"
            : "01-01-entry.html";
    } catch (error) {
        errorEl.innerText = error.status === 401
            ? "Niepoprawny login lub hasło."
            : error.message || "Nieprawidłowy email lub hasło.";
    }
});