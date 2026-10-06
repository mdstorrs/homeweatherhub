import { getMetric, setMetric } from "./main.js";

const celsiusButton = document.getElementById("celsius");
const fahrenheitButton = document.getElementById("fahrenheit");
const selectedUnitDisplay = document.getElementById("selected-unit");

function show(metric) {
  celsiusButton.classList.toggle("selected", metric === 1);
  fahrenheitButton.classList.toggle("selected", metric === 0);
  selectedUnitDisplay.textContent = metric === 1 ? "Metric System Selected" : "Freedom Units Selected";
}

function selectUnit(metric) {
  setMetric(metric);
  show(metric);
}

celsiusButton.addEventListener("click", () => selectUnit(1));
fahrenheitButton.addEventListener("click", () => selectUnit(0));
show(getMetric());
