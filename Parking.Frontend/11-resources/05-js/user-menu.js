document.addEventListener("DOMContentLoaded", () => {
  const menus = document.querySelectorAll("[data-user-menu]");
  if (!menus.length) {
    return;
  }

  const email = localStorage.getItem("loggedInUser") || "";
  const displayEmail = email || "Zaloguj się";

  const closeAll = () => {
    document.querySelectorAll("[data-user-dropdown]").forEach((dropdown) => {
      dropdown.setAttribute("hidden", "");
    });
  };

  document.addEventListener("click", closeAll);
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      closeAll();
    }
  });

  menus.forEach((menu) => {
    const emailEl = menu.querySelector("[data-user-email]");
    const trigger = menu.querySelector("[data-user-trigger]");
    const dropdown = menu.querySelector("[data-user-dropdown]");
    const settingsBtn = menu.querySelector("[data-user-action='settings']");
    const logoutBtn = menu.querySelector("[data-user-action='logout']");

    if (emailEl) {
      emailEl.textContent = displayEmail;
    }

    if (trigger && dropdown) {
      trigger.addEventListener("click", (event) => {
        event.stopPropagation();
        if (!email) {
          window.location.href = "00-01-login.html";
          return;
        }
        const isHidden = dropdown.hasAttribute("hidden");
        closeAll();
        if (isHidden) {
          dropdown.removeAttribute("hidden");
        }
      });
    }

    if (settingsBtn) {
      settingsBtn.addEventListener("click", () => {
        closeAll();
        const role = localStorage.getItem("userRole");
        window.location.href = role === "Operator"
          ? "02-02-operator-settings.html"
          : "01-05-client-account-settings.html";
      });
    }

    if (logoutBtn) {
      logoutBtn.addEventListener("click", () => {
        closeAll();
        if (typeof setAuthToken === "function") {
          setAuthToken(null);
        }
        if (typeof setLoggedInUser === "function") {
          setLoggedInUser(null);
        }
        localStorage.removeItem("authToken");
        localStorage.removeItem("loggedInUser");
        localStorage.removeItem("userRole");
        window.location.href = "index.html";
      });
    }
  });
});
