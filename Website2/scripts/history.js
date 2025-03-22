import { baseUrl } from "./main.js";

startUp();

const params = getParams();

renderHistoryReport();

async function startUp() {

    const stationCurrentDiv = await document.querySelector('.js-station-history');
    const stationNameLabel = await document.querySelector('.js-station-name');

    let reportData = "";
    const data = {}

    reportData += await fillSections(data, "Please wait...", "Loading");

    stationCurrentDiv.innerHTML = reportData;

}

async function renderHistoryReport() 
{
    const stationCurrentDiv = await document.querySelector('.js-station-history');
    const stationNameLabel = await document.querySelector('.js-station-name');

    //make sure we have some sort of params
    if (!params || params.success === false) {
        window.location.href = 'index.html';
        return;
    }

    let stationName = "Weather Station";

    if (!stationCurrentDiv || !stationNameLabel) {
        return;
    }

    const dateRange = getDateRange(params); //Read the params from the request and calulate the date range

    //Get the data from the server
    const data = await getHistoryReport(dateRange);

    //Check for no data

    let reportData = "";

    //make sure there is data here and there were no errros
    if (data.success) {

        stationName = data.wsName;

        if (data.success && data.measurementSymbol == null) {
            dateRange.label = "No Report Data";
        }

        reportData += await fillSections(data, dateRange.label, stationName);
    
    }

    if (!data.success && data.error) {
        stationNameLabel.innerHTML = data.error;
    } else if (!data.success) {
        stationNameLabel.innerHTML = data.message;
    } else {
        //stationNameLabel.innerHTML = stationName;
        stationCurrentDiv.innerHTML = reportData;
    }

}

async function fillSections(data, label, stationName) {

    let reportData = "";

    reportData += await renderDataSection(
        "TEMP", "MIN", "MAX",
        [
            { desc: "OUTSIDE", min: `${data.outsideTemperatureMin || ''}`, max: `${data.outsideTemperatureMax || ''}` },
            { desc: "INSIDE", min: `${data.insideTemperatureMin || ''}`, max: `${data.insideTemperatureMax || ''}` },
        ],
        label, stationName);

    reportData += await renderDataSection(
        "RAIN", "", "MAX",
        [
            { desc: "ACCUM", min: ``, max: `${data.totalRain || ''}` },
            { desc: "RATE", min: ``, max: `${data.rainRateMax || ''}` },
        ],
        null, null);

    reportData += await renderDataSection(
        "WIND", "", "MAX",
        [
            { desc: "MAX. SPEED", min: ``, max: `${data.windSpeedMax || ''}` },
            { desc: "MAX. GUST", min: ``, max: `${data.windGustMax || ''}` },
            { desc: "AVG DIRECTION", min: ``, max: `${data.windDirectionAvg || ''}` },
        ],
        null, null);

    reportData += await renderDataSection(
        "HUMIDITY", "MIN", "MAX",
        [
            { desc: "OUTSIDE", min: `${data.outsideHumidityMin || ''}`, max: `${data.outsideHumidityMax || ''}` },
            { desc: "INSIDE", min: `${data.insideHumidityMin || ''}`, max: `${data.insideHumidityMax || ''}` },
        ],
        "", null);

    reportData += await renderDataSection(
        "MISC", "MIN", "MAX",
        [
            { desc: "PRESSURE", min: `${data.pressureMin || ''}`, max: `${data.pressureMax || ''}` },
            { desc: "UV INDEX", min: ``, max: `${data.uvIndexMax || ''}` },
        ],
        "", null);

    return reportData;

}

function getDateRange(params) {

    const now = new Date(); //This is the date and time
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    
    let fromDate = new Date(today); //default start date. eg. Todays date as date only 2025/03/11 00:00:00 at minight
    let toDate = new Date(today); //default end date. eg. Todays date. We will add later. eg 1 day. 2025/03/12 00:00:00
    let label = "Today";

    switch (params.mode)
    {
        case 1: //7 Days
            const subtractDays = (params.offset * 7) - 1; 
            fromDate.setDate(today.getDate() - subtractDays - 7);
            toDate.setDate(today.getDate() - subtractDays);
            label = `Week from ${fromDate.toLocaleDateString()}`;
            break;
        case 2: //Month
            fromDate = new Date(today.getFullYear(), today.getMonth() - params.offset, 1);
            toDate = new Date(today.getFullYear(), today.getMonth() - params.offset + 1, 1); 
            label = fromDate.toLocaleString('en-US', { month: 'long', year: 'numeric' });
            break;
        case 3: //year
            fromDate = new Date(today.getFullYear() - params.offset, 0, 1);
            toDate = new Date(today.getFullYear() - params.offset + 1, 0, 1); 
            label = today.getFullYear() - params.offset;
            break;
        case 4: //All
            fromDate = new Date(2000, 0, 1 ,0,0,0);
            toDate.setDate(today.getDate() + 1);
            label = "All Time";
            break;
        default: //Day or any errors
            fromDate.setDate(today.getDate() -(params.offset));
            toDate.setDate(today.getDate() -(params.offset) + 1); //DateTime.Now.Date.AddDays(-(params.offset) + 1);
            if (params.offset == 0)
                label = "Today";
            else if (params.offset == 1)
                label = "Yesterday";
            else
                label = `${fromDate.toLocaleDateString()}`;
                //label = `${fromDate.toLocaleDateString()} to ${toDate.toLocaleDateString()}`;
            break;
    }

    return { 
        mode: params.mode + 1,
        offset: params.offset,
        fromDate: fromDate,
        toDate: toDate,
        label: label,
    }; 

}

function getParams() {

    const urlParams = new URLSearchParams(window.location.search);

    const paramId = urlParams.get('id');
    const paramMode = urlParams.get('mode');
    const paramOffset = urlParams.get('offset');
    const paramMetric = urlParams.get('metric');

    //Get local storage ID.
    let id = 0; 
    let mode = 0; 
    let offset = 0;
    let metric = 1 ;

    if (!paramId)
        id = localStorage.getItem('id');
    else
        localStorage.setItem('id', paramId);

    if (!paramMode)
        mode = localStorage.getItem('mode');
    else
        localStorage.setItem('mode', paramMode);

    if (!paramMetric)
        metric = localStorage.getItem('metric');
    else
        localStorage.setItem('metric', paramMetric);

    if (!id) return { success: false, id: 1, mode: 0, offset: 0, metric: 1 } 

    if (!mode)
        mode = 0;

    if (!offset) 
        offset = 0;
        
    if (!metric)
        metric = 1;

    return { success: true, id: id, mode: mode, offset: offset, metric: metric } 

}

async function renderDataSection(title, titleMin, titleMax, dataLines, specialHeader, stationName) {
    let data = `<div class="cs-section-div">`;
    data += `<div class="section-data">`;

    if (stationName) {
        data += `<h2 class="js-station-name">${stationName}</h2>`;
    }

    if (specialHeader) {
        data += `<h3>${specialHeader}</h3>`;
    }

    data += `<div class="data-line">
        <span class="section-header-line">${title}</span><span class="section-header-line-right">${titleMin}</span></span><span class="section-header-line-right">${titleMax}</span>
    </div>`;

    for (const dataline of dataLines) {
        const line = `<div class="data-line"><span class="label">${dataline.desc}</span><span class="value">${dataline.min}</span><span class="value">${dataline.max}</span></div>`;
        data += line;
    }

    //Close it off
    data += `</div></div>`;

    return data;
}

async function getHistoryReport(dataRange) {
    //this filter can be used to filter by name set to a space for now.
    try {

        const dateString = `${dataRange.fromDate.getFullYear()}-${dataRange.fromDate.getMonth() + 1}-${dataRange.fromDate.getDate()}`;

        if (!params || !params.id) {
            throw new Error(`Invalid Station ID`);
        }

        const url = `${baseUrl}History/${params.id}/${dataRange.mode}/${dateString}/${params.metric}/`;

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

/*
function updateMenuLinks() {
    const stationId = localStorage.getItem('id');
    const currentLink = document.getElementById('currentLink');
    const historyLink = document.getElementById('historyLink');
  
    if (stationId) {
      currentLink.classList.remove('disabled-link');
      currentLink.disabled = false;
      historyLink.classList.remove('disabled-link');
      historyLink.disabled = false;
    } else {
      currentLink.classList.add('disabled-link');
      currentLink.disabled = true;
      historyLink.classList.add('disabled-link');
      historyLink.disabled = true;
    }
  }
    */

import { updateMenuLinks } from './main.js';

// Call updateMenuLinks when the page loads
updateMenuLinks();

// Optionally, you can call updateMenuLinks when localStorage changes
window.addEventListener('storage', updateMenuLinks);

document.getElementById("leftButton").addEventListener("click", function() {
    if (!params) return;
    if (!params.offset) {
        params.offset = 1; }
    else { 
        params.offset++; }
    params.mode = parseInt(document.getElementById("historyCombo").value, 10);
    renderHistoryReport();
});

document.getElementById("rightButton").addEventListener("click", function() {
    if (!params) return;
    if (!params.offset || params.offset == 0) {
        params.offset = 0; }
    else {
        params.offset--; }
    params.mode = parseInt(document.getElementById("historyCombo").value, 10);
    renderHistoryReport();
});

document.getElementById("historyCombo").addEventListener("change", function(){
    if (!params) return;
    params.offset = 0;
    params.mode = parseInt(document.getElementById("historyCombo").value, 10);
    renderHistoryReport();
});