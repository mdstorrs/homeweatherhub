import { baseUrl } from "./main.js";

let previousUpdateTime = new Date; // Initialize to null

const params = getParams();

renderCurrentReport();
setInterval(renderCurrentReport, 5000); // Refresh every 5 seconds

async function renderCurrentReport() {

    const stationCurrentDiv = await document.querySelector('.js-station-current');
    const stationNameLabel = await document.querySelector('.js-station-name');
    const statusLabel = await document.querySelector('.js-status-label');
    const lastUpdateLabel = await document.querySelector('.js-last-updated');

    //let id = localStorage.getItem('id');
    let metric = localStorage.getItem('metric');

    if (metric == null) 
        metric = 1;

    if (!params || params.success==false) {
        //window.location.href = 'index.html';
        console.log(params);
        return;
    } else {
        localStorage.setItem("id", params.id);
    }

    let stationName = "Weather Station";

    if (!stationCurrentDiv || !stationNameLabel) {
        return;
    }

    //Get the data from the server
    const data = await getCurrentReport(params.id, metric);

    let currentData = "";
    let lastUpdate = { refreshNeeded: true, timeString: "...", online: false};

    //make sure there is data here and there were no errros
    if (data.success) {

        lastUpdate = getTimeSinceLastUpdate(data.lastUpdated, data.serverTime, previousUpdateTime);

        if (lastUpdate.refreshNeeded) {

            stationName = data.wsName;

            currentData = `
                    <div class="cs-section-div">
                        <h2 class="js-station-name">${stationName}</h2>
                        <h3>Current Contitions</h3>
                        <h4 class="js-last-updated" id="lastUpdatedHeader">${lastUpdate.timeString}</h4>
                        <p><span class="cs-temp">${data.tempOutside}</span><span class="cs-temp-symbol">°</span><span class="cs-temp-unit">${data.measurementSymbol}</span></p>
                        <p>Humidity ${data.humidityOutside}</p>
                    </div>
            `;

            currentData += await renderDataSection(
                "RAIN", "",
                [
                    { desc: "ACCUM", value: `${data.rainAccumulation}` },
                    { desc: "RATE", value: `${data.rainRate}` },
                ]);

            currentData += await renderDataSection(
                "WIND", "",
                [
                    { desc: "DIRECTION", value: `${data.windDirection}` },
                    { desc: "SPEED", value: `${data.windSpeed}` },
                    { desc: "GUSTS", value: `${data.windGust}` },
                ]);

            currentData += await renderDataSection(
                "INSIDE", "",
                [
                    { desc: "TEMP", value: `${data.tempInside}` },
                    { desc: "HUMIDITY", value: `${data.humidityInside}` },
                ]);

            currentData += await renderDataSection(
                "MISC", "",
                [
                    { desc: "PRESSUE", value: `${data.pressure}` },
                    { desc: "UV INDEX", value: `${data.uvIndex}` },
                ]);

        }

        previousUpdateTime = data.lastUpdated;

    }

    const header = document.getElementById("lastUpdatedHeader");

    //If the station is offline set the last updated label to red
    if (lastUpdate && lastUpdate.online === false) {
        if (header) 
            header.style.color = "var(--error)";        
    }

    //I don't know if this is needed, but only update the objects if there is new data.
    if (lastUpdate.refreshNeeded) {
        if (!data.success && data.error) {
            statusLabel.innerHTML = data.error;
        } else if (!data.success) {
            statusLabel.innerHTML = data.message;
        } else {
            stationNameLabel.innerHTML = stationName;
            stationCurrentDiv.innerHTML = currentData;
        }
    } else {
        lastUpdateLabel.innerHTML = lastUpdate.timeString;
    }

}

async function renderDataSection(title, titleVal, dataLines) {
    let data = `<div class="cs-section-div"><div class="section-data">`;

    //Data section titel
    data += `<div class="data-line"><span class="section-header-line">${title}</span><span class="section-header-line-right">${titleVal}</span></div>`;

    for (const dataline of dataLines) {
        const line = `<div class="data-line"><span class="label">${dataline.desc}</span><span class="value">${dataline.value}</span></div>`;
        data += line;
    }

    //Close it off
    data += `</div></div>`;

    return data;
}

async function getCurrentReport(stationId, metric) {
    //this filter can be used to filter by name set to a space for now.
    try {

        if (!stationId) {
            throw new Error(`Invalid Station ID`);
        }

        const url = `${baseUrl}Current/${stationId}/${metric}/`;

        const response = await fetch(url, {
            method: "GET",
            headers: {
                "Content-Type": "application/json"
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP Error: ${response.status}`);
        }

        const result = await response.json();

        return result;

    } catch (ex) {
        const result = {
            error: ex.message,
            success: false,
            message: "Error",
            lastUpdateTime: new Date
        };
        return result;
    }
}

function getTimeSinceLastUpdate(lastUpdateTime, serverTime, previousLastUpdateTime) {

    const currentTime = new Date(serverTime);
    const lastUpdate = new Date(lastUpdateTime);
    let diffInSeconds = Math.round((currentTime - lastUpdate) / 1000);

    let refreshNeeded = false; //use this if we do not need to refresh
    let timeString; //the return string for display
    let online = false;

    if (previousLastUpdateTime && lastUpdateTime !== previousLastUpdateTime) {
        refreshNeeded = true;
    }

    if (diffInSeconds < 60) {
        timeString = `Online (Updated ${diffInSeconds} seconds ago)`;
        online = true;
    } else if (diffInSeconds < 300) {
        const minutes = Math.floor(diffInSeconds / 60);
        timeString = `Online (Updated ${minutes} minutes ago)`;
        online = true;
    } else if (diffInSeconds < 3600) {
        const minutes = Math.floor(diffInSeconds / 60);
        timeString = `Offline (Updated ${minutes} minutes ago)`;
    } else {
        const hours = Math.floor(diffInSeconds / 3600);
        timeString = `Offline (Updated ${hours} hours ago)`;
    }

    return {
        timeString: timeString,
        refreshNeeded: refreshNeeded,
        online: online,
    };

}

function getParams() {

    const urlParams = new URLSearchParams(window.location.search);
    const paramId = urlParams.get('id');

    //Get local storage ID.
    let id = 0; 

    if (!paramId)
        id = localStorage.getItem('id');
    else
        id = paramId;

    if (!id) return { success: false, id: 1 } 

    return { success: true, id: id } 

}

//Click event for hisotyr button at the bottom
document.getElementById("historyButton").addEventListener("click", function() {
    if (params.id) {
        window.location.href = `history.html?id=${params.id}`;
    }
});

// Optionally, you can call updateMenuLinks when localStorage changes
window.addEventListener('storage', updateMenuLinks);

import { updateMenuLinks } from './main.js';

// Call updateMenuLinks when the page loads
updateMenuLinks();
