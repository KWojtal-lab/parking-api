document.getElementById("registration-confirm").addEventListener("click", async function(e) {
    e.preventDefault();

    const email = document.getElementById("client-registration-email").value.trim();
    const password = document.getElementById("client-registration-password").value;
    const passwordRepeat = document.getElementById("client-registration-password-repeat").value;
    const errorEl = document.getElementById("client-registration-error-msg");

    errorEl.innerText = "";

    if (!email || !password) {
        errorEl.innerText = "Wypełnij wszystkie wymagane pola.";
        return;
    }

    if (password !== passwordRepeat) {
        errorEl.innerText = "Hasła nie są takie same.";
        return;
    }

    try {
        await apiFetchAuth("/api/auth/register", {
            method: "POST",
            body: JSON.stringify({ email, password })
        });

        location.href = "00-01-login.html";
    } catch (error) {
        errorEl.innerText = error.message || "Nie udało się zarejestrować.";
    }
});