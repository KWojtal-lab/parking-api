let entryButton;
let entryError;
let vehicleSelect;
let sessionBox;
let sessionPlate;
let sessionSpot;
let sessionStart;
let sessionDuration;
let sessionFee;
let entryForm;
let qrContainer = null;
let qrCodeInstance = null;

const EXIT_PAGE_URL = "http://localhost:3000/01-02-exit.html";

function setEntryError(message) {
    if (entryError) {
        entryError.innerText = message || "";
    }
}

function updateEntryButtonState() {
    if (!entryButton) {
        return;
    }
    entryButton.disabled = false;
    entryButton.classList.remove("enabled");
}

function setSessionField(el, value) {
    if (!el) {
        return;
    }
    el.innerText = value || "—";
}

function renderQrCode(url) {
    if (!qrContainer) return;
    qrContainer.innerHTML = "";
    qrCodeInstance = new QRCode(qrContainer, {
        text: url,
        width: 180,
        height: 180,
        colorDark: "#000000",
        colorLight: "#ffffff",
        correctLevel: QRCode.CorrectLevel.H,
    });
}

function clearQrCode() {
    if (qrContainer) qrContainer.innerHTML = "";
    qrCodeInstance = null;
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

async function loadVehicles() {
    if (!vehicleSelect) {
        return;
    }

    vehicleSelect.innerHTML = "<option value=\"null\">Ładowanie pojazdów...</option>";

    try {
        const payload = await apiFetch("/api/license-plates");
        const plates = extractPlateItems(payload);

        if (!plates.length) {
            vehicleSelect.innerHTML = "<option value=\"null\">Brak zarejestrowanych pojazdów</option>";
            updateEntryButtonState();
            return;
        }

        vehicleSelect.innerHTML = "<option value=\"null\">Wybierz pojazd</option>";
        plates.forEach((item) => {
            const plateNumber = typeof item === "string" ? item : item.plateNumber || item.number || item.plate;
            if (!plateNumber) {
                return;
            }
            const vehicleType = typeof item === "string" ? "Sedan" : (item.vehicleType || item.type || "Sedan");
            const option = document.createElement("option");
            option.value = plateNumber;
            option.dataset.vehicleType = vehicleType;
            option.innerText = plateNumber;
            vehicleSelect.appendChild(option);
        });
    } catch (error) {
        vehicleSelect.innerHTML = "<option value=\"null\">Brak danych</option>";
        setEntryError(error.message || "Nie udało się pobrać pojazdów.");
    } finally {
        updateEntryButtonState();
    }
}

async function loadCurrentSession() {
    if (!sessionBox) {
        return;
    }
    setEntryError("");

    try {
        const payload = await apiFetch("/api/Parking/current-session");
        if (!payload || typeof payload !== "object") {
            sessionBox.hidden = true;
            if (entryForm) {
                entryForm.hidden = false;
            }
            return;
        }

        const plate = payload.plateNumber || payload.plate || payload.vehiclePlate || "—";
        const spot = payload.spotNumber !== undefined && payload.spotNumber !== null ? String(payload.spotNumber) : "—";
        const startedAt = payload.startTime || payload.startedAt || payload.entryTime || payload.startDate;
        const durationValue = payload.currentDuration || payload.duration || payload.minutes || payload.durationMinutes;
        const fee = payload.currentFee || payload.amount || payload.amountToPay || payload.total || payload.price || 0;
        const hasSessionData = payload.sessionId || payload.plateNumber || payload.plate || payload.vehiclePlate;

        if (!hasSessionData) {
            sessionBox.hidden = true;
            if (entryForm) {
                entryForm.hidden = false;
            }
            return;
        }

        setSessionField(sessionPlate, plate);
        setSessionField(sessionSpot, spot);
        setSessionField(sessionStart, formatDateTime(startedAt));
        setSessionField(sessionDuration, typeof durationValue === "string" ? formatDurationString(durationValue) : String(durationValue || "—"));
        setSessionField(sessionFee, fee ? `${fee} zł` : "—");

        sessionBox.hidden = false;
        if (entryForm) {
            entryForm.hidden = true;
        }
        renderQrCode(EXIT_PAGE_URL);
    } catch (error) {
        clearQrCode();
        if (isNoActiveSessionError(error)) {
            setEntryError("");
            sessionBox.hidden = true;
            if (entryForm) {
                entryForm.hidden = false;
            }
            return;
        }
        setEntryError(error.message || "Nie udało się pobrać aktywnej sesji.");
        sessionBox.hidden = true;
        if (entryForm) {
            entryForm.hidden = false;
        }
    }
}

async function submitEntry() {
    setEntryError("");
    const plateNumber = vehicleSelect ? vehicleSelect.value : "";

    if (!plateNumber || plateNumber === "null") {
        setEntryError("Wybierz pojazd.");
        return;
    }

    try {
        await apiFetch("/api/Parking/entry", {
            method: "POST",
            body: JSON.stringify({ plateNumber })
        });
        await loadCurrentSession();
    } catch (error) {
        setEntryError(error.message || "Nie udało się zarejestrować wjazdu.");
    }
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

document.addEventListener("DOMContentLoaded", () => {
    entryButton = document.getElementById("entry-submit-btn");
    entryError = document.getElementById("entry-error-msg");
    vehicleSelect = document.getElementById("carsType");
    sessionBox = document.getElementById("entry-session");
    entryForm = document.getElementById("entry-form");
    sessionPlate = document.getElementById("entry-session-plate");
    sessionSpot = document.getElementById("entry-session-spot");
    sessionStart = document.getElementById("entry-session-start");
    sessionDuration = document.getElementById("entry-session-duration");
    sessionFee = document.getElementById("entry-session-fee");
    qrContainer = document.getElementById("entry-qr-code");

    if (sessionBox) {
        sessionBox.hidden = true;
    }
    if (entryForm) {
        entryForm.hidden = false;
    }

    if (vehicleSelect) {
        vehicleSelect.addEventListener("change", updateEntryButtonState);
    }
    if (entryButton) {
        entryButton.addEventListener("click", submitEntry);
    }

    badgeSystemStatusFunction();
    updateLiveCapacity();
    loadParkingState().then(updateLiveCapacity);
    loadVehicles();
    loadCurrentSession();
});
