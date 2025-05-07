checklogin();

function checklogin() {

  if (!localStorage.getItem('loggedin')) {
    window.location.href = 'login.html';
    return;
  }

}