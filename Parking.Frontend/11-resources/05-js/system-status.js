const parkingState = {
    isActive: true,
    freeSpaces: 20,
    takenSpaces: 30,
    reservedSpaces: 0,
    serviceSpaces: 0,
    totalSpaces: 50,
    updatedAt: 0
};

function getStateField(payload, keys, fallback) {
    for (const key of keys) {
        if (payload && Object.prototype.hasOwnProperty.call(payload, key)) {
            return payload[key];
        }
    }
    return fallback;
}

async function loadParkingState() {
    try {
        const payload = await apiFetch("/api/parking/state", { skipAuthRedirect: true });
        const takenSpots = Array.isArray(payload && payload.takenSpots) ? payload.takenSpots : null;
        const hasStatus = payload && ("isActive" in payload || "active" in payload || "status" in payload);
        const total = Number(getStateField(payload, ["totalSpaces", "total", "capacity"], parkingState.totalSpaces));

        let taken = takenSpots ? takenSpots.length
            : Number(getStateField(payload, ["takenSpaces", "occupied", "busy"], parkingState.takenSpaces));
        if (!Number.isFinite(taken)) {
            taken = parkingState.takenSpaces;
        }

        let free = Number(getStateField(payload, ["freeSpaces", "free", "available"], parkingState.freeSpaces));
        if (!Number.isFinite(free)) {
            free = Number.isFinite(total) ? Math.max(total - taken, 0) : parkingState.freeSpaces;
        }

        const reserved = Number(getStateField(payload, ["reservedSpaces", "reserved"], parkingState.reservedSpaces));
        const service = Number(getStateField(payload, ["serviceSpaces", "service"], parkingState.serviceSpaces));

        parkingState.isActive = hasStatus
            ? Boolean(getStateField(payload, ["isActive", "active", "status"], parkingState.isActive))
            : true;
        parkingState.totalSpaces = Number.isFinite(total) ? total : parkingState.totalSpaces;
        parkingState.takenSpaces = Math.max(taken, 0);
        parkingState.freeSpaces = Math.max(free, 0);
        parkingState.reservedSpaces = Number.isFinite(reserved) ? reserved : parkingState.reservedSpaces;
        parkingState.serviceSpaces = Number.isFinite(service) ? service : parkingState.serviceSpaces;
        parkingState.updatedAt = Date.now();
    } catch (error) {
        if (!error || (error.status !== 401 && error.status !== 403)) {
            console.warn("Parking state load failed", error);
        }
    }

    return parkingState;
}

function renderBadgeStatus() {
    const badge = document.getElementById("badge");
    if (!badge) {
        return;
    }
    let text = "<span class=\"dot-green\"></span>";
    if (checkServerStatus()) {
        text += getFreeParkingSpaces() + " wolnych miejsc &mdash; Parking dostępny";
        badge.classList.remove("inactive");
    } else {
        text += "Parking niedostępny";
        badge.classList.add("inactive");
    }
    badge.innerHTML = text;
}

function renderIndexStatus() {
    const indexStatus = document.getElementById("index-system-status");
    if (!indexStatus) {
        return;
    }
    let text = "<span class=\"dot-live\"></span>";
    if (checkServerStatus()) {
        indexStatus.classList.remove("inactive");
        text += "System aktywny &middot; " + getAllParkingSpaces() + " miejsc parkingowych.";
    } else {
        text += "System nieaktywny";
        indexStatus.classList.add("inactive");
    }
    indexStatus.innerHTML = text;
}

function badgeSystemStatusFunction() {
    renderBadgeStatus();
    loadParkingState().then(renderBadgeStatus);
}

function indexSystemStatusFunction() {
    renderIndexStatus();
    loadParkingState().then(renderIndexStatus);
}

// -- Data -- //
function checkServerStatus() {
    return parkingState.isActive;
}

function getFreeParkingSpaces() {
    return parkingState.freeSpaces;
}

function getTakenParkingSpaces() {
    return parkingState.takenSpaces;
}

function getServiseParkingSpaces() {
    return parkingState.serviceSpaces;
}

function getReservedParkingSpaces() {
    return parkingState.reservedSpaces;
}

function getAllParkingSpaces() {
    return parkingState.totalSpaces;
}

function getTodayMoneyIntake() {
    return 0;
}