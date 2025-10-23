// Theme toggle
(function () {
  const body = document.body;
  const btn = document.getElementById("theme-toggle");
  const KEY = "ws-theme";
  const saved = localStorage.getItem(KEY);
  if (saved === "light" || saved === "dark") {
    body.setAttribute("data-theme", saved);
    if (btn) btn.textContent = saved === "dark" ? "🌙" : "☀️";
  }
  btn?.addEventListener("click", () => {
    const next = body.getAttribute("data-theme") === "dark" ? "light" : "dark";
    body.setAttribute("data-theme", next);
    localStorage.setItem(KEY, next);
    btn.textContent = next === "dark" ? "🌙" : "☀️";
  });
})();

// Tabs
(function () {
  const tabbar = document.getElementById("ws-tabs");
  if (!tabbar) return;
  const tabs = Array.from(tabbar.querySelectorAll("[data-tab]"));
  const panes = Array.from(document.querySelectorAll(".ws-tabpane"));
  function activate(name) {
    tabs.forEach((t) =>
      t.classList.toggle("ws-tab--active", t.getAttribute("data-tab") === name)
    );
    panes.forEach((p) =>
      p.classList.toggle(
        "ws-tabpane--active",
        p.getAttribute("data-tab") === name
      )
    );
  }
  tabs.forEach((t) =>
    t.addEventListener("click", () => activate(t.getAttribute("data-tab")))
  );
})();

// Geolocation: fill hidden inputs and submit
(function () {
  const btn = document.getElementById("geo-btn");
  const form = document.getElementById("geo-form");
  if (!btn || !form) return;
  const latInput = document.getElementById("geo-lat");
  const lonInput = document.getElementById("geo-lon");
  btn.addEventListener("click", () => {
    if (!navigator.geolocation) {
      alert("Geolocation is not supported by your browser.");
      return;
    }
    btn.disabled = true;
    btn.textContent = "Detecting…";
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const { latitude, longitude } = pos.coords;
        latInput.value = String(latitude);
        lonInput.value = String(longitude);
        form.submit();
      },
      (err) => {
        alert("Failed to detect location: " + err.message);
        btn.disabled = false;
        btn.textContent = "Detect & Fetch";
      },
      { enableHighAccuracy: true, timeout: 10000 }
    );
  });
})();
