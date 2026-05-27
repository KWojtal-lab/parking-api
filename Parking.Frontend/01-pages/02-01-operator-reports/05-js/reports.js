let revenueChart    = null;
let sessionsChart   = null;
let popularityChart = null;
let popGrid         = null;

const CHART_PALETTE = {
    blue:      "#3B82F6",
    blueArea:  "rgba(59, 130, 246, 0.15)",
    indigo:    "#6366F1",
    indigoBar: "rgba(99, 102, 241, 0.75)",
    gridLine:  "rgba(107, 114, 128, 0.15)",
    tick:      "#6B7280",
};

// ── helpers ───────────────────────────────────────────────────────────────────

function formatDateLabel(value) {
    if (!value) return "";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    const day   = String(date.getDate()).padStart(2, "0");
    const month = String(date.getMonth() + 1).padStart(2, "0");
    return `${day}.${month}`;
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

function setKpiValue(id, value) {
    const el = document.getElementById(id);
    if (el) el.innerText = value;
}

function getKpiRevenue() {
    const value  = document.getElementById("reports-total-revenue")?.innerText || "0";
    const parsed = Number(String(value).replace(/[^0-9.-]/g, ""));
    return Number.isNaN(parsed) ? 0 : parsed;
}

function parseTimeSpanToMinutes(value) {
    if (!value) return null;
    if (typeof value === "number" && Number.isFinite(value)) return value / 60000;
    if (typeof value !== "string") return null;
    const normalized = value.trim();
    if (!normalized) return null;
    const daySplit  = normalized.split(".");
    const hasDays   = daySplit.length === 2;
    const days      = hasDays ? Number(daySplit[0]) : 0;
    const timePart  = hasDays ? daySplit[1] : normalized;
    const timeParts = timePart.split(":").map(Number);
    if (timeParts.length < 2 || timeParts.some(Number.isNaN)) return null;
    const [hours = 0, minutes = 0, seconds = 0] = timeParts;
    return (days * 24 * 60) + (hours * 60) + minutes + (seconds / 60);
}

function formatAvgDuration(minutes) {
    if (!Number.isFinite(minutes) || minutes <= 0) return "—";
    const total = Math.round(minutes);
    const hours = Math.floor(total / 60);
    const mins  = total % 60;
    if (hours > 0 && mins > 0) return `${hours}h ${mins}min`;
    if (hours > 0)             return `${hours}h`;
    return `${mins}min`;
}

// ── Chart.js factory ──────────────────────────────────────────────────────────

function makeChart(canvasId, existing, config) {
    if (existing) existing.destroy();
    const canvas = document.getElementById(canvasId);
    if (!canvas) return null;
    return new Chart(canvas, config);
}

function axisOpts(extraTicks = {}) {
    return {
        grid:  { color: CHART_PALETTE.gridLine },
        ticks: { color: CHART_PALETTE.tick, ...extraTicks },
    };
}

// ── charts ────────────────────────────────────────────────────────────────────

async function loadRevenue() {
    const payload = await apiFetch("/api/operator/statistics/revenue?period=monthly");
    const points  = Array.isArray(payload && payload.points) ? payload.points : [];
    const values  = points.map(p => p.totalRevenue   || 0);
    const labels  = points.map(p => formatDateLabel(p.date));

    const total = values.reduce((s, v) => s + v, 0);
    setKpiValue("reports-total-revenue", `${total.toFixed(2)} zł`);

    revenueChart = makeChart("canvas-revenue", revenueChart, {
        type: "line",
        data: {
            labels,
            datasets: [{
                data:             values,
                borderColor:      CHART_PALETTE.blue,
                backgroundColor:  CHART_PALETTE.blueArea,
                fill:             true,
                tension:          0.3,
                pointRadius:      3,
                pointHoverRadius: 5,
            }],
        },
        options: {
            responsive:          true,
            maintainAspectRatio: false,
            plugins: {
                legend:  { display: false },
                tooltip: { callbacks: { label: ctx => `${ctx.parsed.y.toFixed(2)} zł` } },
            },
            scales: {
                x: axisOpts({ maxRotation: 45, autoSkip: true, maxTicksLimit: 10 }),
                y: { ...axisOpts(), beginAtZero: true, ticks: { color: CHART_PALETTE.tick, callback: v => `${v} zł` } },
            },
        },
    });
}

async function loadSessions() {
    const payload = await apiFetch("/api/operator/statistics/sessions?period=monthly");
    const points  = Array.isArray(payload && payload.points) ? payload.points : [];
    const values  = points.map(p => p.totalSessions || 0);
    const labels  = points.map(p => formatDateLabel(p.date));

    const total = values.reduce((s, v) => s + v, 0);
    setKpiValue("reports-total-sessions", total);

    const avg = total > 0 ? (getKpiRevenue() / total) : 0;
    setKpiValue("reports-avg-revenue", total ? `${avg.toFixed(2)} zł` : "—");

    sessionsChart = makeChart("canvas-sessions", sessionsChart, {
        type: "bar",
        data: {
            labels,
            datasets: [{
                data:            values,
                backgroundColor: CHART_PALETTE.indigoBar,
                borderColor:     CHART_PALETTE.indigo,
                borderWidth:     1,
                borderRadius:    4,
            }],
        },
        options: {
            responsive:          true,
            maintainAspectRatio: false,
            plugins: {
                legend:  { display: false },
                tooltip: { callbacks: { label: ctx => `${ctx.parsed.y} sesji` } },
            },
            scales: {
                x: axisOpts({ maxRotation: 45, autoSkip: true, maxTicksLimit: 10 }),
                y: { ...axisOpts(), beginAtZero: true },
            },
        },
    });
}

async function loadPopularity() {
    const payload = await apiFetch("/api/operator/statistics/spot-popularity");
    const spots   = Array.isArray(payload && payload.spots) ? payload.spots : [];
    const sorted  = [...spots].sort((a, b) => (b.occupancyCount || 0) - (a.occupancyCount || 0));
    const top30   = sorted.slice(0, 30);

    popularityChart = makeChart("canvas-popularity", popularityChart, {
        type: "bar",
        data: {
            labels: top30.map(item => `#${item.spotNumber}`),
            datasets: [{
                data: top30.map(item => item.occupancyCount || 0),
                backgroundColor: top30.map((_, i) => {
                    const ratio = i / Math.max(1, top30.length - 1);
                    const r = Math.round(99  + (236 - 99)  * ratio);
                    const g = Math.round(102 + (72  - 102) * ratio);
                    const b = Math.round(241 + (153 - 241) * ratio);
                    return `rgba(${r}, ${g}, ${b}, 0.8)`;
                }),
                borderRadius: 4,
            }],
        },
        options: {
            indexAxis:           "y",
            responsive:          true,
            maintainAspectRatio: false,
            plugins: {
                legend:  { display: false },
                tooltip: { callbacks: { label: ctx => `${ctx.parsed.x} wizyt` } },
            },
            scales: {
                x: { ...axisOpts(), beginAtZero: true },
                y: axisOpts(),
            },
        },
    });

    renderPopularityGrid(spots);
}

async function loadAverageDuration() {
    try {
        const payload = await apiFetch("/api/operator/parking/history");
        const items   = Array.isArray(payload) ? payload : [];
        const now     = Date.now();
        const cutoff  = now - (30 * 24 * 60 * 60 * 1000);

        const durations = items.map(item => {
            const endValue = item.endTime || item.exitTime || item.endedAt || item.endDate;
            const endDate  = endValue ? new Date(endValue) : null;
            if (!endDate || Number.isNaN(endDate.getTime()) || endDate.getTime() < cutoff) return null;
            return parseTimeSpanToMinutes(item.totalDuration || item.duration || item.totalTime || item.sessionDuration);
        }).filter(v => Number.isFinite(v));

        if (!durations.length) { setKpiValue("reports-avg-duration", "—"); return; }

        const total = durations.reduce((s, v) => s + v, 0);
        setKpiValue("reports-avg-duration", formatAvgDuration(total / durations.length));
    } catch (error) {
        console.warn("Average duration load failed", error);
        setKpiValue("reports-avg-duration", "—");
    }
}

// ── heatmapa — bez zmian ──────────────────────────────────────────────────────

function renderPopularityGrid(items) {
    if (!popGrid) return;
    popGrid.innerHTML = "";
    const max = Math.max(1, ...items.map(item => item.occupancyCount || 0));
    const map = new Map(items.map(item => [item.spotNumber, item.occupancyCount]));
    for (let i = 1; i <= 50; i++) {
        const count = map.get(i) || 0;
        const ratio = count / max;
        const hue   = Math.round(60 * (1 - ratio));
        const sat   = count === 0 ? 15 : 90;
        const light = count === 0 ? 94 : Math.round(68 - ratio * 22);
        const cell  = document.createElement("div");
        cell.className       = "pop-cell";
        cell.style.background = `hsl(${hue}, ${sat}%, ${light}%)`;
        cell.textContent     = i;
        popGrid.appendChild(cell);
    }
}

// ── tabs ──────────────────────────────────────────────────────────────────────

function switchTab(tab) {
    const isIncome = tab === "income";
    document.getElementById("panel-income").style.display = isIncome ? "" : "none";
    document.getElementById("panel-pop").style.display    = isIncome ? "none" : "";
    document.getElementById("tab-income").classList.toggle("active",  isIncome);
    document.getElementById("tab-pop").classList.toggle("active",    !isIncome);
    if (!isIncome && popularityChart) popularityChart.resize();
}

window.switchTab = switchTab;

// ── init ──────────────────────────────────────────────────────────────────────

document.addEventListener("DOMContentLoaded", () => {
    popGrid = document.getElementById("pop-grid");

    updateLiveCapacity();
    loadParkingState().then(updateLiveCapacity);

    loadRevenue().then(loadSessions).catch(error => console.warn("Reports load failed", error));
    loadAverageDuration().catch(error => console.warn("Average duration load failed", error));
    loadPopularity().catch(error => console.warn("Popularity load failed", error));
});
