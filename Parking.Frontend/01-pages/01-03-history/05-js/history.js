let historyContainer;
let historyError;

function setHistoryError(message) {
	if (historyError) {
		historyError.innerText = message || "";
	}
}

function formatDate(value) {
	if (!value) {
		return "—";
	}
	const date = new Date(value);
	if (Number.isNaN(date.getTime())) {
		return "—";
	}
	return date.toLocaleString("pl-PL");
}

function extractHistoryItems(payload) {
	if (Array.isArray(payload)) {
		return payload;
	}
	if (!payload || typeof payload !== "object") {
		return [];
	}
	const preferredKeys = ["items", "history", "sessions", "data", "result", "value"];
	for (const key of preferredKeys) {
		if (Array.isArray(payload[key])) {
			return payload[key];
		}
	}
	const arrays = Object.values(payload).filter(Array.isArray);
	if (!arrays.length) {
		return [];
	}
	const likely = arrays.find((arr) => arr.some((item) => item && (item.plateNumber || item.entryTime || item.startTime || item.totalFee)));
	return likely || arrays[0];
}

function renderHistory(items) {
	if (!historyContainer) {
		return;
	}
	if (!items.length) {
		historyContainer.innerHTML = "<div class=\"history-empty\">Brak historii</div>";
		return;
	}

	const rows = items.map((item) => {
		const plate = item.plateNumber || item.plate || item.number || item.licensePlate || "—";
		const entry = formatDate(item.startTime || item.entryTime || item.startedAt || item.entryDate);
		const exit = formatDate(item.endTime || item.exitTime || item.endedAt || item.exitDate);
		const amount = item.totalFee || item.amount || item.total || item.price || item.totalAmount || 0;
		return `
			<div class="history-row">
				<div><strong>${plate}</strong></div>
				<div>Wjazd: ${entry}</div>
				<div>Wyjazd: ${exit}</div>
				<div>Kwota: ${amount} zł</div>
			</div>
		`;
	}).join("");

	historyContainer.innerHTML = rows;
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

async function loadHistory() {
	setHistoryError("");
	try {
		const payload = await apiFetch("/api/Parking/history");
		const items = extractHistoryItems(payload);
		renderHistory(items);
	} catch (error) {
		setHistoryError(error.message || "Nie udało się pobrać historii.");
	}
}

document.addEventListener("DOMContentLoaded", () => {
	historyContainer = document.getElementById("parking-history");
	historyError = document.getElementById("history-error-msg");

	updateLiveCapacity();
	loadParkingState().then(updateLiveCapacity);
	loadHistory();
});
