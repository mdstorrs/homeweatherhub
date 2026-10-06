import { getJson, escapeHtml, getStationId, getMetric, setTitle } from "./main.js";

// Stations post every 30-60 seconds, so checking more often only adds load.
const REFRESH_MS = 30000;
// A reading older than this means the station is offline.
const OFFLINE_AFTER_SECONDS = 300;

const stationId = getStationId();
const container = document.querySelector(".js-station-current");

let lastData = null;      // last successful response, kept on screen if a refresh fails
let fetchedAt = 0;        // when lastData arrived (browser clock)
let refreshTimer = null;
let ageTimer = null;
let loading = false;

if (!stationId) {
  window.location.replace("index.html"); // no station chosen yet
} else {
  document.getElementById("historyButton")?.addEventListener("click", () => {
    window.location.href = `history.html?id=${stationId}`;
  });
  start();
  // Don't poll in a background tab; catch up as soon as it's visible again.
  document.addEventListener("visibilitychange", () => (document.hidden ? stop() : start()));
}

function start() {
  stop();
  refresh();
  refreshTimer = setInterval(refresh, REFRESH_MS);
  ageTimer = setInterval(updateAge, 10000);
}

function stop() {
  clearInterval(refreshTimer);
  clearInterval(ageTimer);
}

async function refresh() {
  if (loading) return;
  loading = true;
  const data = await getJson(`Current/${stationId}/${getMetric()}/`);
  loading = false;

  if (data.success) {
    lastData = data;
    fetchedAt = Date.now();
    render(null);
  } else {
    render(data.error || data.message || "Unable to load current conditions.");
  }
}

// Seconds since the station's last reading. Both times come from the server's clock, so the phone's
// time zone doesn't matter; the time since we fetched is added so the text keeps ticking between refreshes.
function ageSeconds() {
  const serverTime = new Date(lastData.serverTime);
  const lastUpdated = new Date(lastData.lastUpdated);
  if (isNaN(serverTime) || isNaN(lastUpdated)) return null;
  return Math.max(0, Math.round((serverTime - lastUpdated + (Date.now() - fetchedAt)) / 1000));
}

function describeAge(seconds) {
  if (seconds === null) return { text: "", online: true };
  const online = seconds < OFFLINE_AFTER_SECONDS;
  let ago;
  if (seconds < 60) ago = `${seconds} seconds ago`;
  else if (seconds < 3600) ago = `${Math.floor(seconds / 60)} minute${seconds < 120 ? "" : "s"} ago`;
  else if (seconds < 172800) ago = `${Math.floor(seconds / 3600)} hour${seconds < 7200 ? "" : "s"} ago`;
  else ago = `${Math.floor(seconds / 86400)} days ago`;
  return { text: `${online ? "Online" : "Offline"} (updated ${ago})`, online };
}

function updateAge() {
  const header = document.getElementById("lastUpdatedHeader");
  if (!header || !lastData) return;
  const age = describeAge(ageSeconds());
  header.textContent = age.text;
  header.classList.toggle("cs-offline", !age.online);
}

function render(error) {
  if (!container) return;

  if (!lastData) {
    // Nothing to show yet: just the error (the page retries automatically).
    container.innerHTML = `
      <div class="cs-section-div">
        <h2 class="js-station-name">Weather Station</h2>
        <h3 class="cs-error">${escapeHtml(error)}</h3>
        <h4>Retrying automatically...</h4>
      </div>`;
    return;
  }

  const d = lastData;
  setTitle(d.wsName);
  const age = describeAge(ageSeconds());

  let html = `
    <div class="cs-section-div">
      <h2 class="js-station-name">${escapeHtml(d.wsName)}</h2>
      <h3>Current Conditions</h3>
      <h4 class="js-last-updated${age.online ? "" : " cs-offline"}" id="lastUpdatedHeader">${escapeHtml(age.text)}</h4>
      ${error ? `<p class="cs-error">${escapeHtml(error)} Showing the last reading.</p>` : ""}
      <p><span class="cs-temp">${escapeHtml(d.tempOutside)}</span><span class="cs-temp-symbol">°</span><span class="cs-temp-unit">${escapeHtml(d.measurementSymbol)}</span></p>
      <p>Humidity ${escapeHtml(d.humidityOutside)}</p>
    </div>`;

  html += section("RAIN", [["ACCUM", d.rainAccumulation], ["RATE", d.rainRate]]);
  html += section("WIND", [["DIRECTION", d.windDirection], ["SPEED", d.windSpeed], ["GUSTS", d.windGust]]);
  const unit = d.measurementSymbol ? ` °${d.measurementSymbol}` : "";
  html += section("INSIDE", [["TEMP", d.tempInside != null ? `${d.tempInside}${unit}` : null], ["HUMIDITY", d.humidityInside]]);
  html += section("MISC", [["PRESSURE", d.pressure], ["UV INDEX", d.uvIndex]]);

  container.innerHTML = html;
}

function section(title, rows) {
  let html = `<div class="cs-section-div"><div class="section-data">`;
  html += `<div class="data-line"><span class="section-header-line">${title}</span><span class="section-header-line-right"></span></div>`;
  for (const [label, value] of rows) {
    html += `<div class="data-line"><span class="label">${label}</span><span class="value">${escapeHtml(value ?? "-")}</span></div>`;
  }
  return html + `</div></div>`;
}
