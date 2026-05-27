let carNumberInput;
let carTypeSelect;
let carSubmitBtn;
let carErrorEl;
let carSuccessEl;
let carListEl;
let notificationBtn;
let currentCars = [];
let editingPlate = null;

const FALLBACK_VEHICLE_TYPES = [
	"Sedan",
	"SUV",
	"Combi",
	"Hatchback",
	"Crossover",
	"Coupe",
	"Van",
	"Kabriolet"
];

function setCarError(message) {
	if (carErrorEl) {
		carErrorEl.innerText = message || "";
	}
}

function setCarSuccess(message) {
	if (carSuccessEl) {
		carSuccessEl.innerText = message || "";
	}
}

function setApiError(error, fallback) {
	if (error && error.status === 400) {
		setCarError(error.message || "Niepoprawne dane wejściowe.");
		return true;
	}
	setCarError(error && error.message ? error.message : fallback);
	return false;
}

function getVehicleTypes() {
	if (carTypeSelect && carTypeSelect.options && carTypeSelect.options.length) {
		return Array.from(carTypeSelect.options).map((option) => option.value);
	}
	return FALLBACK_VEHICLE_TYPES;
}

function extractPlateItems(payload) {
	if (Array.isArray(payload)) {
		return payload;
	}
	if (!payload || typeof payload !== "object") {
		return [];
	}
	const preferredKeys = ["items", "licensePlates", "plates", "plateNumbers", "data", "result", "value"];
	for (const key of preferredKeys) {
		if (Array.isArray(payload[key])) {
			return payload[key];
		}
	}
	const arrays = Object.values(payload).filter(Array.isArray);
	if (!arrays.length) {
		return [];
	}
	const likely = arrays.find((arr) => arr.some((item) => typeof item === "string" || (item && (item.plateNumber || item.number || item.plate))));
	return likely || arrays[0];
}

function escapeHtml(value) {
	return String(value)
		.replace(/&/g, "&amp;")
		.replace(/</g, "&lt;")
		.replace(/>/g, "&gt;")
		.replace(/\"/g, "&quot;")
		.replace(/'/g, "&#39;");
}

function toDataPlate(plateNumber) {
	return encodeURIComponent(plateNumber);
}

function fromDataPlate(value) {
	return decodeURIComponent(value || "");
}

function renderCarList(plates) {
	if (!carListEl) {
		return;
	}
	if (!plates.length) {
		carListEl.innerHTML = "<div class=\"car-empty\">Brak zarejestrowanych pojazdów.</div>";
		return;
	}
	if (editingPlate && !plates.some((item) => item.plateNumber === editingPlate)) {
		editingPlate = null;
	}

	const typeOptions = getVehicleTypes();

	carListEl.innerHTML = plates.map((car) => {
		const plate = escapeHtml(car.plateNumber);
		const typeLabel = escapeHtml(car.vehicleType || "Sedan");
		const dataPlate = toDataPlate(car.plateNumber);

		if (editingPlate === car.plateNumber) {
			const optionsMarkup = typeOptions.map((type) => {
				const selected = type === car.vehicleType ? " selected" : "";
				return `<option value=\"${escapeHtml(type)}\"${selected}>${escapeHtml(type)}</option>`;
			}).join("");

			return `
				<div class=\"car-row\" data-plate=\"${dataPlate}\">
					<div class=\"car-edit\">
						<input type=\"text\" class=\"car-edit-plate\" value=\"${plate}\" />
						<select class=\"car-edit-type\">${optionsMarkup}</select>
					</div>
					<div class=\"car-actions\">
						<button type=\"button\" class=\"btn-blue btn-small\" data-action=\"save\">Zapisz</button>
						<button type=\"button\" class=\"btn-blue btn-small\" data-action=\"cancel\">Anuluj</button>
					</div>
				</div>
			`;
		}

		return `
			<div class=\"car-row\" data-plate=\"${dataPlate}\">
				<div class=\"car-info\">
					<div class=\"car-plate\">${plate}</div>
					<div class=\"car-type\">${typeLabel}</div>
				</div>
				<div class=\"car-actions\">
					<button type=\"button\" class=\"btn-blue btn-small\" data-action=\"edit\">Edytuj</button>
					<button type=\"button\" class=\"btn-danger btn-small\" data-action=\"delete\">Usuń</button>
				</div>
			</div>
		`;
	}).join("");
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

async function loadCars() {
	setCarError("");
	try {
		const payload = await apiFetch("/api/license-plates");
		const plates = extractPlateItems(payload);
		const list = plates.map((item) => {
			if (typeof item === "string") {
				return { plateNumber: item, vehicleType: "Sedan" };
			}
			const plateNumber = item.plateNumber || item.number || item.plate;
			if (!plateNumber) {
				return null;
			}
			return { plateNumber, vehicleType: item.vehicleType || item.type || "Sedan" };
		}).filter(Boolean);

		currentCars = list;
		renderCarList(currentCars);
	} catch (error) {
		setCarError(error.message || "Nie udało się pobrać listy pojazdów.");
	}
}

async function handleAddCar(event) {
	if (event) {
		event.preventDefault();
	}
	setCarError("");
	setCarSuccess("");

	const plateNumber = carNumberInput ? carNumberInput.value.trim() : "";
	const vehicleType = carTypeSelect ? carTypeSelect.value : "Sedan";

	if (!plateNumber) {
		setCarError("Podaj numer rejestracyjny.");
		return;
	}

	try {
		await apiFetch("/api/license-plates", {
			method: "POST",
			body: JSON.stringify({ plateNumber, vehicleType })
		});

		setCarSuccess("Pojazd został dodany.");
		if (carNumberInput) {
			carNumberInput.value = "";
		}
		await loadCars();
	} catch (error) {
		setApiError(error, "Nie udało się dodać pojazdu.");
	}
}


async function handleDeleteCar(plateNumber) {
	setCarError("");
	setCarSuccess("");

	try {
		await apiFetch(`/api/license-plates/${encodeURIComponent(plateNumber)}`, {
			method: "DELETE"
		});

		setCarSuccess("Pojazd został usunięty.");
		await loadCars();
	} catch (error) {
		setApiError(error, "Nie udało się usunąć pojazdu.");
	}
}

async function handleSaveCarEdit(oldPlate, newPlate, newType) {
	setCarError("");
	setCarSuccess("");

	if (!newPlate) {
		setCarError("Podaj numer rejestracyjny.");
		return;
	}

	try {
		await apiFetch(`/api/license-plates/${encodeURIComponent(oldPlate)}`, {
			method: "PUT",
			body: JSON.stringify({ newPlateNumber: newPlate, vehicleType: newType })
		});

		editingPlate = null;
		setCarSuccess("Pojazd został zaktualizowany.");
		await loadCars();
	} catch (error) {
		setApiError(error, "Nie udało się zaktualizować pojazdu.");
	}
}

async function handleCarListClick(event) {
	const button = event.target.closest("button[data-action]");
	if (!button) {
		return;
	}
	const row = button.closest(".car-row");
	if (!row) {
		return;
	}
	const plateNumber = fromDataPlate(row.dataset.plate);
	const action = button.dataset.action;

	if (action === "edit") {
		setCarError("");
		setCarSuccess("");
		editingPlate = plateNumber;
		renderCarList(currentCars);
		return;
	}

	if (action === "delete") {
		await handleDeleteCar(plateNumber);
		return;
	}

	if (action === "save") {
		const plateInput = row.querySelector(".car-edit-plate");
		const typeSelect = row.querySelector(".car-edit-type");
		const newPlate = plateInput ? plateInput.value.trim() : "";
		const newType = typeSelect ? typeSelect.value : "Sedan";
		await handleSaveCarEdit(plateNumber, newPlate, newType);
		return;
	}

	if (action === "cancel") {
		editingPlate = null;
		await loadCars();
	}
}

async function handleNotificationClick(event) {
	if (event) {
		event.preventDefault();
	}
	setCarError("");
	setCarSuccess("");

	try {
		const payload = await apiFetch("/api/notifications");
		const message = payload && payload.message ? payload.message : "Pobrano powiadomienia.";
		setCarSuccess(message);
	} catch (error) {
		setApiError(error, "Nie udało się pobrać powiadomień.");
	}
}

document.addEventListener("DOMContentLoaded", () => {
	carNumberInput = document.getElementById("carNumber");
	carTypeSelect = document.getElementById("carsType");
	carSubmitBtn = document.getElementById("car-submit");
	carErrorEl = document.getElementById("car-error-msg");
	carSuccessEl = document.getElementById("car-success-msg");
	carListEl = document.getElementById("car-list");
	notificationBtn = document.getElementById("notification-button");

	if (carSubmitBtn) {
		carSubmitBtn.addEventListener("click", handleAddCar);
	}
	if (notificationBtn) {
		notificationBtn.addEventListener("click", handleNotificationClick);
	}
	if (carListEl) {
		carListEl.addEventListener("click", handleCarListClick);
	}

	updateLiveCapacity();
	loadParkingState().then(updateLiveCapacity);
	loadCars();
});
