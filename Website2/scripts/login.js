// Accounts aren't available yet (the login system is planned for after the move to the new host).
// Until then the forms are switched off, so nothing is ever submitted. In particular, the browser's
// default form submission would put the password in the page address.

for (const form of document.querySelectorAll("form")) {
  form.addEventListener("submit", (e) => e.preventDefault());
  for (const field of form.querySelectorAll("input, button")) {
    field.disabled = true;
  }

  const notice = document.createElement("p");
  notice.className = "cs-notice";
  notice.textContent = "Accounts are coming soon. You can browse every weather station without one.";
  form.before(notice);
}
