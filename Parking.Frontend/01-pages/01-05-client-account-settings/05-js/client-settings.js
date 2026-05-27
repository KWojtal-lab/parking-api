const balanceEl         = document.getElementById("account-balance");
const balanceInput      = document.getElementById("balance-amount");
const balanceBtn        = document.getElementById("balance-add-btn");
const logoutBtn         = document.getElementById("logout-btn");
const changePasswordBtn = document.getElementById("change-password-btn");
const currentPasswordEl = document.getElementById("current-password");
const newPasswordEl     = document.getElementById("new-password");
const repeatPasswordEl  = document.getElementById("repeat-password");
const accountError      = document.getElementById("account-error-msg");
const accountSuccess    = document.getElementById("account-success-msg");

function setAccountError(message) {
	if (accountError) accountError.innerText = message || "";
	if (accountSuccess) accountSuccess.innerText = "";
}

function setAccountSuccess(message) {
	if (accountSuccess) accountSuccess.innerText = message || "";
	if (accountError) accountError.innerText = "";
}

function updateLiveCapacity() {
	const container = document.getElementById("live-cappacity");
	if (!container) return;
	const bar        = '<div class="cap-bar"><div class="cap-bar-fill"></div></div>';
	const total      = getAllParkingSpaces();
	const taken      = getTakenParkingSpaces();
	const percentage = total > 0 ? Math.round(taken / total * 100) : 0;
	container.innerHTML = `<span class="dot-live"></span>Live: ${taken}/${total}${bar}${percentage}%`;
	const fill = container.querySelector(".cap-bar-fill");
	if (fill) fill.style.width = `${percentage}%`;
}

async function loadBalance() {
	setAccountError("");
	try {
		const payload = await apiFetch("/api/users/money");
		const amount  = payload && (payload.amount ?? payload.balance ?? payload.money ?? payload.value);
		balanceEl.innerText = amount !== undefined ? `${amount} zł` : "—";
	} catch (error) {
		setAccountError(error.message || "Nie udało się pobrać salda.");
	}
}

async function addBalance() {
	setAccountError("");
	setAccountSuccess("");

	const amount = Number(balanceInput.value);
	if (!amount || amount <= 0) {
		setAccountError("Podaj poprawną kwotę.");
		return;
	}

	try {
		await apiFetch("/api/users/money/add", {
			method: "POST",
			body: JSON.stringify({ amount })
		});
		setAccountSuccess("Środki zostały dodane.");
		balanceInput.value = "";
		await loadBalance();
	} catch (error) {
		setAccountError(error.message || "Nie udało się dodać środków.");
	}
}

async function changePassword() {
	setAccountError("");
	setAccountSuccess("");

	const currentPassword = currentPasswordEl ? currentPasswordEl.value : "";
	const newPassword     = newPasswordEl     ? newPasswordEl.value     : "";
	const repeatPassword  = repeatPasswordEl  ? repeatPasswordEl.value  : "";

	if (!currentPassword) { setAccountError("Podaj aktualne hasło."); return; }
	if (!newPassword)     { setAccountError("Podaj nowe hasło.");      return; }
	if (newPassword !== repeatPassword) {
		setAccountError("Hasła nie są identyczne.");
		return;
	}

	try {
		await apiFetchAuth("/api/auth/change-password", {
			method: "POST",
			body: JSON.stringify({ currentPassword, newPassword })
		});
		setAccountSuccess("Hasło zostało zmienione. Za chwilę zostaniesz wylogowany...");
		setTimeout(logout, 1500);
	} catch (error) {
		setAccountError(error.message || "Nie udało się zmienić hasła.");
	}
}

function logout() {
	setAuthToken(null);
	setLoggedInUser(null);
	setUserRole(null);
	window.location.href = "index.html";
}

balanceBtn.addEventListener("click", addBalance);
logoutBtn.addEventListener("click", logout);
changePasswordBtn.addEventListener("click", changePassword);

document.addEventListener("DOMContentLoaded", () => {
	updateLiveCapacity();
	loadParkingState().then(updateLiveCapacity);
	loadBalance();
});
