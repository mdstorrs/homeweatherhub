// Shared by every page: API access, the selected station, units, escaping and the side menu.

export const baseUrl = "https://api.homeweatherhub.com/";

// GET JSON from the API. Never throws: failures come back as { success: false, error }.
// No Content-Type header on purpose: on a GET it makes the browser send an extra CORS "preflight"
// request before every call, doubling the number of requests.
export async function getJson(path) {
  try {
    const response = await fetch(baseUrl + path, { cache: "no-store" });
    if (!response.ok) {
      throw new Error(`The weather server returned an error (HTTP ${response.status}). Please try again later.`);
    }
    return await response.json();
  } catch (ex) {
    const offline = ex instanceof TypeError; // fetch reports network failures as TypeError
    return {
      success: false,
      message: "Error",
      error: offline ? "Unable to reach the weather server. Check your internet connection." : ex.message
    };
  }
}

// Text from the API (e.g. station names, which users will be able to set) must never be treated as HTML.
export function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}

function readStorage(key) {
  try { return localStorage.getItem(key); } catch { return null; }
}

function writeStorage(key, value) {
  try { localStorage.setItem(key, value); } catch { /* private browsing: just don't remember */ }
}

// The station to show: ?id= in the address, otherwise the last one chosen. Only whole numbers are accepted.
export function getStationId() {
  const fromUrl = new URLSearchParams(window.location.search).get("id");
  if (fromUrl && /^\d+$/.test(fromUrl)) {
    writeStorage("id", fromUrl);
    return fromUrl;
  }
  const saved = readStorage("id");
  return saved && /^\d+$/.test(saved) ? saved : null;
}

export function setStationId(id) {
  writeStorage("id", String(id));
}

// 1 = metric (default), 0 = imperial.
export function getMetric() {
  return readStorage("metric") === "0" ? 0 : 1;
}

export function setMetric(metric) {
  writeStorage("metric", metric === 0 ? "0" : "1");
}

export function setTitle(stationName) {
  document.title = stationName ? `Home Weather Hub - ${stationName}` : "Home Weather Hub";
}

// ---------- Side menu ----------

function openNav() {
  document.getElementById("mySidebar")?.classList.add("open");
  document.getElementById("main")?.classList.add("open");
}

function closeNav() {
  document.getElementById("mySidebar")?.classList.remove("open");
  document.getElementById("main")?.classList.remove("open");
}

function isNavOpen() {
  return document.getElementById("mySidebar")?.classList.contains("open") ?? false;
}

// Current and History links carry the selected station, and are disabled until one is chosen.
export function updateMenuLinks() {
  const stationId = getStationId();
  for (const [elementId, page] of [["currentLink", "current.html"], ["historyLink", "history.html"]]) {
    const link = document.getElementById(elementId);
    if (!link) continue;
    if (stationId) {
      link.classList.remove("disabled-link");
      link.removeAttribute("aria-disabled");
      link.href = `${page}?id=${stationId}`;
    } else {
      link.classList.add("disabled-link");
      link.setAttribute("aria-disabled", "true");
      link.href = page;
    }
  }
}

document.querySelector(".openbtn")?.addEventListener("click", (e) => { e.stopPropagation(); openNav(); });
document.querySelector(".closebtn")?.addEventListener("click", (e) => { e.preventDefault(); closeNav(); });

// Close the menu with Escape or by clicking anywhere outside it.
document.addEventListener("keydown", (e) => { if (e.key === "Escape" && isNavOpen()) closeNav(); });
document.addEventListener("click", (e) => {
  if (isNavOpen() && !document.getElementById("mySidebar")?.contains(e.target)) closeNav();
});

updateMenuLinks();
window.addEventListener("storage", updateMenuLinks); // another tab chose a different station
