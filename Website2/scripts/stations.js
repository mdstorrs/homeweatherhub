
import { baseUrl } from "./main.js";

checkParams();

function checkParams() {

  const urlParams = new URLSearchParams(window.location.search);

  const paramId = urlParams.get('id');

  if (paramId) {
    showCurrent(paramId);
    return;
  }

  renderStations();

}

async function renderStations() {

  const stationListDiv = await document.querySelector('.js-station-list');

  if (!stationListDiv) { return; }

  const data = await getStationList();

  //make sure there is data here and there were no errros
  if (data.success && data.stations) {

    const stations = data.stations;

    for (const station of stations) {
      // Create a button element to convert to a html string later
      const button = document.createElement("button");
      button.className = "main-menu-button";
      button.dataset.id = station.id; // Store ID in data-id attribute
      button.innerHTML = `<h2>${station.name}</h2><p>${station.address}</p><h4>${station.coordinates}</h4>`;
  
      // Attach event listener to call the function with the station ID
      button.addEventListener("click", function () {
        showCurrent(this.dataset.id); // Call your function with the stored ID
      });

      //append object to div
      stationListDiv.appendChild(button);

    }
  } else if (!data.success && data.error) {
    stationListDiv.innerHTML = `<p>${data.error}</p>`;
  }

}

function showCurrent(id) {
  localStorage.setItem('id', id);
  window.location.href = `current.html?id=${id}`;
}

async function getStationList() {

  //this filter can be used to filter by name set to a space for now.
  const filter = "";
  const page = 0;
  const stationsPerPage = 0;

  const url = `${baseUrl}Stations/${page}/${stationsPerPage}/${filter}`; 

  try {
      const response = await fetch(url, {
          method: "GET",
          headers: {
              "Content-Type": "application/json"
          }
      });

      if (!response.ok) {
          throw new Error(`HTTP Error. Status: ${response.status}`);
      }

      const result = await response.json();

      //console.log(result);

      return result;

  } catch (ex) {
      const result = {
        error: ex.message,
        success: false,
        message: "Error",
        stations: [],
        totalPages: 1,
        totalCount: 0
      };
      //document.getElementById("response").innerText = `{error: ${error.message}}`;
      return result;
  }

}

import { updateMenuLinks } from './main.js';

// Call updateMenuLinks when the page loads
updateMenuLinks();

// Optionally, you can call updateMenuLinks when localStorage changes
window.addEventListener('storage', updateMenuLinks);
