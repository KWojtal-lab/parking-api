let exitTimeEl;
let exitDateEl;
let exitPlateEl;
let exitAmountEl;
let exitButton;
let exitErrorEl;
let exitSuccessEl;

function setExitError(message) {
	if (exitErrorEl) {
		exitErrorEl.innerText = message || "";
	}
}

function setExitSuccess(message) {
	if (exitSuccessEl) {
		exitSuccessEl.innerText = message || "";
	}
}

function formatDateTime(value) {
	if (!value) {
		return "—";
	}
	const date = new Date(value);
	if (Number.isNaN(date.getTime())) {
		return "—";
	}
	return date.toLocaleString("pl-PL");
}

function formatDurationMinutes(minutes) {
	if (minutes === null || minutes === undefined || Number.isNaN(minutes)) {
		return "—";
	}
	const hours = Math.floor(minutes / 60);
	const mins = Math.floor(minutes % 60);
	if (hours > 0) {
		return `${hours}h ${mins}min`;
	}
	return `${mins}min`;
}

function formatDurationString(value) {
	if (!value || typeof value !== "string") {
		return "—";
	}
	const parts = value.split(":");
	if (parts.length < 3) {
		return value;
	}
	const [dayPart, hourPart] = parts[0].includes(".") ? parts[0].split(".") : ["0", parts[0]];
	const days = Number(dayPart);
	const hours = Number(hourPart);
	const minutes = Number(parts[1]);
	if ([days, hours, minutes].some((item) => Number.isNaN(item))) {
		return value;
	}
	const chunks = [];
	if (days > 0) {
		chunks.push(`${days}d`);
	}
	chunks.push(`${hours}h`);
	chunks.push(`${minutes}min`);
	return chunks.join(" ");
}

function isNoActiveSessionMessage(message) {
	return typeof message === "string" && message.toLowerCase().includes("no active session");
}

function isNoActiveSessionError(error) {
	if (!error) {
		return false;
	}
	if (error.status === 404 || error.status === 204) {
		return true;
	}
	const message = typeof error.message === "string" ? error.message.toLowerCase() : "";
	if (message.includes("not found") || message.includes("404")) {
		return true;
	}
	return isNoActiveSessionMessage(error.message);
}

function updateLiveCapacity() {
	const container = document.getElementById("live-cappacity");
	if (!container) {
		return;
	}
	const bar = '<div class="cap-bar"><div class="cap-bar-fill"></div></div>';
	const total = getAllParkingSpaces();
	const taken = getTakenParkingSpaces();
	const percentage = total > 0 ? Math.round(taken / total * 100) : 0;
	container.innerHTML = `<span class="dot-live"></span>Live: ${taken}/${total}${bar}${percentage}%`;
	const fill = container.querySelector(".cap-bar-fill");
	if (fill) {
		fill.style.width = `${percentage}%`;
	}
}

async function loadCurrentSession() {
	setExitError("");
	setExitSuccess("");
	if (exitButton) {
		exitButton.disabled = true;
		exitButton.hidden = true;
	}

	try {
		const payload = await apiFetch("/api/Parking/current-session");
		if (!payload || typeof payload !== "object") {
			exitPlateEl.innerText = "Brak zaparkowanego pojazdu.";
			exitAmountEl.innerText = "—";
			exitTimeEl.innerText = "—";
			exitDateEl.innerText = "—";
			return;
		}

		const plate = payload.plateNumber || payload.plate || payload.vehiclePlate || "—";
		const startedAt = payload.startTime || payload.startedAt || payload.entryTime || payload.startDate;
		const durationValue = payload.currentDuration || payload.durationMinutes || payload.minutes || payload.duration;
		const amount = payload.currentFee || payload.amount || payload.amountToPay || payload.price || payload.total || 0;

		exitPlateEl.innerText = plate;
		exitAmountEl.innerText = amount ? `${amount} zł` : "—";
		if (typeof durationValue === "string") {
			exitTimeEl.innerText = formatDurationString(durationValue);
		} else {
			exitTimeEl.innerText = durationValue ? formatDurationMinutes(Number(durationValue)) : "—";
		}
		exitDateEl.innerText = formatDateTime(startedAt);
		if (!payload.sessionId && !payload.plateNumber && !payload.plate && !payload.vehiclePlate) {
			exitPlateEl.innerText = "Brak zaparkowanego pojazdu.";
			exitAmountEl.innerText = "—";
			exitTimeEl.innerText = "—";
			exitDateEl.innerText = "—";
			return;
		}

		if (exitButton) {
			exitButton.disabled = false;
			exitButton.hidden = false;
		}
	} catch (error) {
		if (isNoActiveSessionError(error)) {
			setExitError("");
			return;
		}
		setExitError(error.message || "Nie udało się pobrać sesji.");
	}
}

async function handleExit() {
	setExitError("");
	setExitSuccess("");
	exitButton.disabled = true;

	try {
		await apiFetch("/api/Parking/exit", { method: "POST" });
		setExitSuccess("Płatność przyjęta. Możesz wyjechać.");
		await loadCurrentSession();
		window.location.href = "01-01-entry.html";
	} catch (error) {
		setExitError(error.message || "Nie udało się zakończyć postoju.");
	} finally {
		exitButton.disabled = false;
	}
}

document.addEventListener("DOMContentLoaded", () => {
	exitTimeEl = document.getElementById("exit-time");
	exitDateEl = document.getElementById("exit-date");
	exitPlateEl = document.getElementById("exit-plate");
	exitAmountEl = document.getElementById("exit-amount");
	exitButton = document.getElementById("exit-pay-btn");
	exitErrorEl = document.getElementById("exit-error-msg");
	exitSuccessEl = document.getElementById("exit-success-msg");

	if (exitButton) {
		exitButton.hidden = true;
	}

	if (exitButton) {
		exitButton.addEventListener("click", handleExit);
	}

	updateLiveCapacity();
	loadParkingState().then(updateLiveCapacity);
	loadCurrentSession();
});
