const TOTAL_SPOTS = 50;
const DISABLED_SPOTS = new Set([49, 50]);

let grid = null;
let refreshButton = null;

function renderGrid(takenSpots) {
    if (!grid) {
        return;
    }
    grid.innerHTML = "";
    for (let i = 1; i <= TOTAL_SPOTS; i++) {
        const isOccupied = takenSpots.has(i);
        const isDisabled = DISABLED_SPOTS.has(i);
        const cell = document.createElement("a");
        cell.href = `02-03-spot-detail.html?spot=${i}&occupied=${isOccupied ? "1" : "0"}`;
        const classes = ["spot"];
        if (isDisabled) {
            classes.push("disabled");
            if (isOccupied) classes.push("occupied");
        } else {
            classes.push(isOccupied ? "occupied" : "free");
        }
        cell.className = classes.join(" ");
        cell.textContent = i;
        cell.title = `Miejsce #${i}${isDisabled ? " (niepełnosprawni)" : ""}`;
        grid.appendChild(cell);
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

function updateOccupancyUI() {
    const total = getAllParkingSpaces();
    const takenPct = total > 0 ? Math.round(getTakenParkingSpaces() / total * 100) : 0;
    const percentage = takenPct + "%";
    const pctEl = document.querySelector(".pct");
    const capFill = document.querySelector(".cap-bar-fill");
    const occFill = document.querySelector(".occ-fill");

    if (pctEl) {
        pctEl.innerText = percentage;
    }
    if (capFill) {
        capFill.style.width = percentage;
    }
    if (occFill) {
        occFill.style.width = percentage;
    }

    const freeEl = document.getElementById("free");
    const takenEl = document.getElementById("taken");
    const reservedEl = document.getElementById("reserved");
    const moneyEl = document.getElementById("money");

    if (freeEl) {
        freeEl.innerText = getFreeParkingSpaces();
    }
    if (takenEl) {
        takenEl.innerText = getTakenParkingSpaces();
    }
    if (reservedEl) {
        reservedEl.innerText = getReservedParkingSpaces();
    }
    if (moneyEl) {
        moneyEl.innerText = "—";
    }
}

async function loadRevenueKpi() {
    try {
        const payload = await apiFetch("/api/operator/statistics/revenue?period=monthly");
        const points = Array.isArray(payload && payload.points) ? payload.points : [];
        const total = points.reduce((sum, point) => sum + (point.totalRevenue || 0), 0);
        const moneyEl = document.getElementById("money");
        if (moneyEl) {
            moneyEl.innerText = `${total} zł`;
        }
    } catch (error) {
        console.warn("Revenue KPI load failed", error);
    }
}

async function loadParkingStateForDashboard() {
    try {
        const payload = await apiFetch("/api/parking/state");
        const takenSpots = new Set(Array.isArray(payload && payload.takenSpots) ? payload.takenSpots : []);
        renderGrid(takenSpots);

        const total = getAllParkingSpaces ? getAllParkingSpaces() : TOTAL_SPOTS;
        const free = Math.max(total - takenSpots.size, 0);

        parkingState.takenSpaces = takenSpots.size;
        parkingState.freeSpaces = free;
        parkingState.totalSpaces = total;
        parkingState.isActive = true;

        updateLiveCapacity();
        updateOccupancyUI();
    } catch (error) {
        console.warn("Dashboard state load failed", error);
        if (grid) {
            grid.innerHTML = `<p class="grid-error">Nie udało się załadować stanu parkingu (${error.status ? "HTTP " + error.status + ": " : ""}${error.message || "błąd sieci"})</p>`;
        }
    }
}

async function refreshDashboard() {
    await loadParkingStateForDashboard();
    await loadRevenueKpi();
}

document.addEventListener("DOMContentLoaded", () => {
    grid = document.getElementById("parking-grid");
    refreshButton = document.querySelector(".btn-blue");

    if (refreshButton) {
        refreshButton.addEventListener("click", refreshDashboard);
    }

    updateLiveCapacity();
    updateOccupancyUI();
    refreshDashboard();
});
