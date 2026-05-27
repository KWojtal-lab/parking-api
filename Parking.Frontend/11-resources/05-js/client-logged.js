let user = localStorage.getItem("loggedInUser");

if (!user) {
    window.location.href = "index.html";
}

document.getElementById(client-logout-button).addEventListener("click", function() {
    localStorage.removeItem("loggedInUser");
    window.location.href = "index.html";
});