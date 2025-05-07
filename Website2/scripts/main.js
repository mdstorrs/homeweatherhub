export const baseUrl = "https://api.homeweatherhub.com/"

function openNav() {
  document.getElementById("mySidebar").classList.add("open");
  document.getElementById("main").classList.add("open");
}

function closeNav() {
  document.getElementById("mySidebar").classList.remove("open");
  document.getElementById("main").classList.remove("open");
}

// Add event listeners
document.addEventListener('DOMContentLoaded', function() {
  const openBtn = document.querySelector('.openbtn');
  const closeBtn = document.querySelector('.closebtn');

  if (openBtn) {
      openBtn.addEventListener('click', openNav);
  }

  if (closeBtn) {
      closeBtn.addEventListener('click', closeNav);
  }
});


export function updateMenuLinks() {
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
