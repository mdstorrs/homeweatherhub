
function LoadSettings() {

  let metric = localStorage.getItem('metric');

  if (metric == 0) {
    selectUnit(0);
  }
  else {
    selectUnit(1);
  }

}

function selectUnit(metric) {

  const celsiusButton = document.getElementById('celsius');
  const fahrenheitButton = document.getElementById('fahrenheit');
  const selectedUnitDisplay = document.getElementById('selected-unit');

  if (metric == 1) {
    celsiusButton.classList.add('selected');
    fahrenheitButton.classList.remove('selected');
    selectedUnitDisplay.textContent = 'Metric System Selected';
    localStorage.setItem('metric', 1);
  } else {
    fahrenheitButton.classList.add('selected');
    celsiusButton.classList.remove('selected');
    selectedUnitDisplay.textContent = 'Freedom Units Selected';
    localStorage.setItem('metric', 0);
  }

}

LoadSettings();

