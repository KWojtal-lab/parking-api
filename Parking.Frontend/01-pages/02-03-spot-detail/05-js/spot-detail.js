let spotNumberEl = null;
let spotStatusEl = null;
let spotPhotoEl = null;
let sessionPlateEl = null;
let sessionVehicleEl = null;
let sessionDriverEl = null;
let sessionDurationEl = null;
let sessionStartEl = null;
let sessionFeeEl = null;
let historyContainer = null;
let historyCountEl = null;
let markFreeBtn = null;
let occupiedSection = null;

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

function formatDuration(value) {
    if (!value) {
        return "—";
    }
    if (typeof value !== "string") {
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

function renderHistory(items) {
    if (!historyContainer) {
        return;
    }
    historyContainer.innerHTML = "";
    if (!items.length) {
        historyContainer.innerText = "Brak historii";
        if (historyCountEl) {
            historyCountEl.innerText = "0 SESJI";
        }
        return;
    }

    if (historyCountEl) {
        historyCountEl.innerText = `${items.length} SESJI`;
    }

    const rows = items.map((item) => {
        const plate = item.plateNumber || "—";
        const start = formatDateTime(item.startTime);
        const end = formatDateTime(item.endTime);
        return `
            <div class="history-row">
                <span class="plate">${plate}</span>
                <div class="dates"><strong>${start.split(",")[0]}</strong> ${start.split(",")[1] || ""}</div>
                <div class="dates"><strong>${end.split(",")[0]}</strong> ${end.split(",")[1] || ""}</div>
            </div>
        `;
    }).join("");

    historyContainer.innerHTML = rows;
}

function setOccupied(occupied) {
    if (spotStatusEl) {
        if (occupied) {
            spotStatusEl.innerText = "Zajęte";
            spotStatusEl.classList.remove("badge-free");
            spotStatusEl.classList.add("badge-occupied");
        } else {
            spotStatusEl.innerText = "Wolne";
            spotStatusEl.classList.remove("badge-occupied");
            spotStatusEl.classList.add("badge-free");
        }
    }
    if (occupiedSection) {
        occupiedSection.style.display = occupied ? "" : "none";
    }
}

function renderPhoto(url) {
    if (!spotPhotoEl) {
        return;
    }
    if (!url) {
        spotPhotoEl.innerText = "Brak zdjęcia";
        return;
    }
    spotPhotoEl.innerHTML = "";
    const img = document.createElement("img");
    img.src = url;
    img.alt = "Zdjęcie pojazdu";
    img.className = "spot-photo-img";
    spotPhotoEl.appendChild(img);
}

function setSessionDefaults() {
    if (sessionPlateEl) sessionPlateEl.innerText = "—";
    if (sessionVehicleEl) sessionVehicleEl.innerText = "—";
    if (sessionDriverEl) sessionDriverEl.innerText = "—";
    if (sessionDurationEl) sessionDurationEl.innerText = "—";
    if (sessionStartEl) sessionStartEl.innerText = "—";
    if (sessionFeeEl) sessionFeeEl.innerText = "—";
}

function getSpotNumber() {
    const params = new URLSearchParams(window.location.search);
    return Number(params.get("spot")) || 0;
}

function getSpotOccupiedFromUrl() {
    const params = new URLSearchParams(window.location.search);
    const val = params.get("occupied");
    return val === null ? null : val === "1";
}

async function loadSpotData(spotNumber, occupiedHint) {
    setSessionDefaults();

    if (occupiedHint !== null) {
        setOccupied(occupiedHint);
    }

    if (spotNumberEl) {
        spotNumberEl.innerText = `Miejsce #${spotNumber}`;
    }

    // Current session info
    try {
        const spot = await apiFetch(`/api/operator/Parking/${spotNumber}`);
        setOccupied(true);
        if (sessionPlateEl) sessionPlateEl.innerText = spot.plateNumber || "—";
        if (sessionVehicleEl) sessionVehicleEl.innerText = spot.vehicleType || "—";
        if (sessionDriverEl) sessionDriverEl.innerText = spot.driverEmail || "—";
        if (sessionStartEl) sessionStartEl.innerText = formatDateTime(spot.startTime);
        if (sessionDurationEl) sessionDurationEl.innerText = formatDuration(spot.currentDuration);
        if (sessionFeeEl) sessionFeeEl.innerText = spot.currentFee !== undefined ? `${spot.currentFee} zł` : "—";
        if (markFreeBtn) markFreeBtn.disabled = false;
    } catch (error) {
        if (error.status === 404) {
            setOccupied(false);
        } else {
            console.warn("Spot data load failed", error);
        }
        if (markFreeBtn) markFreeBtn.disabled = true;
    }

    // Camera image (only for occupied spots)
    try {
        const camera = await apiFetch(`/api/operator/Parking/camera/spot/${spotNumber}`);
        renderPhoto(camera.cameraImageUrl);
    } catch (error) {
        console.warn("Camera load failed", error);
    }

    // History (always shown regardless of occupancy)
    try {
        const history = await apiFetch(`/api/operator/Parking/history/spot/${spotNumber}`);
        renderHistory(Array.isArray(history) ? history : []);
    } catch (error) {
        console.warn("History load failed", error);
        if (historyContainer) {
            historyContainer.innerText = `Nie udało się załadować historii (${error.status ? "HTTP " + error.status + ": " : ""}${error.message || "błąd sieci"})`;
        }
    }
}

async function handleMarkAsFree() {
    const spotNumber = getSpotNumber();
    const errorEl = document.getElementById("mark-free-error");
    if (errorEl) errorEl.style.display = "none";
    if (markFreeBtn) markFreeBtn.disabled = true;
    try {
        await apiFetch(`/api/operator/Parking/mark-as-free/${spotNumber}`, { method: "PATCH" });
        window.location.href = "02-00-operator-dashboard.html";
    } catch (error) {
        console.warn("Mark as free failed", error);
        if (markFreeBtn) markFreeBtn.disabled = false;
        if (errorEl) {
            errorEl.textContent = error.message || "Nie udało się oznaczyć miejsca jako wolne.";
            errorEl.style.display = "block";
        }
    }
}

document.addEventListener("DOMContentLoaded", () => {
    spotNumberEl = document.getElementById("spot-num");
    spotStatusEl = document.getElementById("spot-status");
    spotPhotoEl = document.getElementById("spot-photo");
    sessionPlateEl = document.getElementById("session-plate");
    sessionVehicleEl = document.getElementById("session-vehicle");
    sessionDriverEl = document.getElementById("session-driver");
    sessionDurationEl = document.getElementById("session-duration");
    sessionStartEl = document.getElementById("session-start");
    sessionFeeEl = document.getElementById("session-fee");
    historyContainer = document.getElementById("spot-history");
    historyCountEl = document.getElementById("history-count");
    markFreeBtn = document.getElementById("mark-free-btn");
    occupiedSection = document.getElementById("occupied-section");

    if (markFreeBtn) {
        markFreeBtn.addEventListener("click", handleMarkAsFree);
    }

    updateLiveCapacity();
    loadParkingState().then(updateLiveCapacity);

    const spotNumber = getSpotNumber();
    const occupiedHint = getSpotOccupiedFromUrl();
    if (spotNumber) {
        loadSpotData(spotNumber, occupiedHint);
    }
});
