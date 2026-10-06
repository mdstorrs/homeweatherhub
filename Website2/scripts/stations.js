import { getJson, setStationId } from "./main.js";

// A link like index.html?id=1 goes straight to that station.
const linkedId = new URLSearchParams(window.location.search).get("id");
if (linkedId && /^\d+$/.test(linkedId)) {
  showCurrent(linkedId);
} else {
  renderStations();
}

function showCurrent(id) {
  setStationId(id);
  window.location.href = `current.html?id=${id}`;
}

function message(listDiv, text, isError) {
  listDiv.replaceChildren();
  const p = document.createElement("p");
  p.textContent = text;
  if (isError) p.className = "cs-error";
  listDiv.appendChild(p);
}

async function renderStations() {
  const listDiv = document.querySelector(".js-station-list");
  if (!listDiv) return;

  message(listDiv, "Loading stations...");

  // Page 1, up to 100 stations (the API returns 10 when no page size is given).
  const data = await getJson("Stations/1/100/");

  if (!data.success && data.error) {
    message(listDiv, data.error, true);
    return;
  }

  const stations = data.stations ?? [];
  if (stations.length === 0) {
    message(listDiv, "No weather stations found.");
    return;
  }

  listDiv.replaceChildren();
  for (const station of stations) {
    // Built with textContent, never innerHTML: station names will be entered by users.
    const button = document.createElement("button");
    button.className = "main-menu-button";
    button.type = "button";

    const name = document.createElement("h2");
    name.textContent = station.name || "Weather station";
    button.appendChild(name);

    if (station.address && station.address.replace(/[\s,]/g, "") !== "") {
      const address = document.createElement("p");
      address.textContent = station.address;
      button.appendChild(address);
    }

    // Hide placeholder coordinates such as "0, 0" or ", ".
    const coordinates = (station.coordinates ?? "").trim();
    if (coordinates && !/^[\s,0.-]*$/.test(coordinates)) {
      const coords = document.createElement("h4");
      coords.textContent = coordinates;
      button.appendChild(coords);
    }

    button.addEventListener("click", () => showCurrent(station.id));
    listDiv.appendChild(button);
  }
}
