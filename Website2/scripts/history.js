import { getJson, escapeHtml, getStationId, getMetric, setTitle } from "./main.js";

// Periods, in the same order as the dropdown. The API's report type is index + 1.
const PERIODS = ["Day", "Week", "Month", "Year", "All"];

const stationId = getStationId();
const container = document.querySelector(".js-station-history");
const combo = document.getElementById("historyCombo");
const leftButton = document.getElementById("leftButton");
const rightButton = document.getElementById("rightButton");

// Period and offset come from the address (so refresh and Back keep them): history.html?id=1&mode=2&offset=3
const query = new URLSearchParams(window.location.search);
const state = {
  mode: clampInt(query.get("mode"), 0, PERIODS.length - 1, 0),
  offset: clampInt(query.get("offset"), 0, 100000, 0) // 0 = the current period, 1 = the one before, ...
};

let stationName = "Weather Station";
let shown = {};        // the report currently on screen
let requestNumber = 0; // only the latest request may update the page

if (!stationId) {
  window.location.replace("index.html"); // no station chosen yet
} else {
  combo.value = String(state.mode);

  leftButton.addEventListener("click", () => { state.offset++; load(); });
  rightButton.addEventListener("click", () => { if (state.offset > 0) { state.offset--; load(); } });
  combo.addEventListener("change", () => { state.mode = parseInt(combo.value, 10); state.offset = 0; load(); });

  load();
}

function clampInt(text, min, max, fallback) {
  const n = parseInt(text, 10);
  return Number.isInteger(n) && n >= min && n <= max ? n : fallback;
}

async function load() {
  const range = getDateRange(state.mode, state.offset);
  const thisRequest = ++requestNumber;

  // Keep the address in step, so refreshing or sharing the link shows the same period.
  history.replaceState(null, "", `history.html?id=${stationId}&mode=${state.mode}&offset=${state.offset}`);

  rightButton.disabled = state.offset === 0 || state.mode === 4; // can't go past today; "All" has no pages
  leftButton.disabled = state.mode === 4;

  // While loading, the previous numbers stay (dimmed) under the new period's name, so the page doesn't flicker.
  container.classList.add("cs-loading");
  render(shown, range.label, "Loading...");

  const data = await getJson(`History/${stationId}/${state.mode + 1}/${toApiDate(range.fromDate)}/${getMetric()}/`);
  if (thisRequest !== requestNumber) return; // the user has already moved to another period
  container.classList.remove("cs-loading");

  if (!data.success) {
    shown = {};
    render(shown, range.label, data.error || data.message || "Unable to load history.");
    return;
  }

  stationName = data.wsName || stationName;
  setTitle(data.wsName);
  shown = data;
  // A station with no readings in the period comes back without a measurement symbol.
  render(shown, range.label, data.measurementSymbol == null ? "No data for this period." : null);
}

function toApiDate(date) {
  const pad = (n) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

function getDateRange(mode, offset) {
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

  switch (mode) {
    case 1: { // Week, Monday to Sunday
      const from = new Date(today);
      const daysSinceMonday = (today.getDay() + 6) % 7;
      from.setDate(today.getDate() - daysSinceMonday - offset * 7);
      const to = new Date(from);
      to.setDate(from.getDate() + 6);
      const thisWeek = offset === 0 ? "This Week" : offset === 1 ? "Last Week" : null;
      const dates = `${from.toLocaleDateString()} to ${to.toLocaleDateString()}`;
      return { fromDate: from, label: thisWeek ? `${thisWeek} (${dates})` : dates };
    }
    case 2: { // Month
      const from = new Date(today.getFullYear(), today.getMonth() - offset, 1);
      return { fromDate: from, label: from.toLocaleDateString(undefined, { month: "long", year: "numeric" }) };
    }
    case 3: { // Year
      const from = new Date(today.getFullYear() - offset, 0, 1);
      return { fromDate: from, label: String(from.getFullYear()) };
    }
    case 4: // All
      return { fromDate: today, label: "All Time" };
    default: { // Day
      const from = new Date(today);
      from.setDate(today.getDate() - offset);
      const label = offset === 0 ? "Today" : offset === 1 ? "Yesterday" : from.toLocaleDateString();
      return { fromDate: from, label };
    }
  }
}

// ?? rather than ||, so a genuine 0 (e.g. UV index 0) is shown instead of a blank.
function value(v) {
  return escapeHtml(v ?? "");
}

function render(d, label, status) {
  let html = section("TEMP", "MIN", "MAX", [
    ["OUTSIDE", d.outsideTemperatureMin, d.outsideTemperatureMax],
    ["INSIDE", d.insideTemperatureMin, d.insideTemperatureMax]
  ], label, status);
  html += section("RAIN", "", "MAX", [["ACCUM", null, d.totalRain], ["RATE", null, d.rainRateMax]]);
  html += section("WIND", "", "MAX", [
    ["MAX. SPEED", null, d.windSpeedMax], ["MAX. GUST", null, d.windGustMax], ["AVG DIRECTION", null, d.windDirectionAvg]
  ]);
  html += section("HUMIDITY", "MIN", "MAX", [
    ["OUTSIDE", d.outsideHumidityMin, d.outsideHumidityMax],
    ["INSIDE", d.insideHumidityMin, d.insideHumidityMax]
  ]);
  html += section("MISC", "MIN", "MAX", [["PRESSURE", d.pressureMin, d.pressureMax], ["UV INDEX", null, d.uvIndexMax]]);
  container.innerHTML = html;
}

// The first section also carries the station name, the period and any status message.
function section(title, minTitle, maxTitle, rows, label, status) {
  let html = `<div class="cs-section-div"><div class="section-data">`;
  if (label !== undefined) {
    html += `<h2 class="js-station-name">${escapeHtml(stationName)}</h2>`;
    html += `<h3>${escapeHtml(label)}</h3>`;
    if (status) html += `<p class="${status === "Loading..." ? "cs-muted" : "cs-error"}">${escapeHtml(status)}</p>`;
  }
  html += `<div class="data-line"><span class="section-header-line">${title}</span>` +
          `<span class="section-header-line-right">${minTitle}</span><span class="section-header-line-right">${maxTitle}</span></div>`;
  for (const [rowLabel, min, max] of rows) {
    html += `<div class="data-line"><span class="label">${rowLabel}</span>` +
            `<span class="value">${value(min)}</span><span class="value">${value(max)}</span></div>`;
  }
  return html + `</div></div>`;
}
