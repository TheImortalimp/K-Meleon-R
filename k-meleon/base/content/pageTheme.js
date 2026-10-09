// Applies the K-Meleon-R theme (kmeleon.display.theme) to internal pages.
(function () {
  function resolve() {
    let mode = "dark";
    try { mode = Services.prefs.getCharPref("kmeleon.display.theme"); } catch (e) {}
    if (mode == "system") {
      try { mode = Services.prefs.getCharPref("kmeleon.theme.effective"); } catch (e) { mode = "dark"; }
    }
    return mode == "light" ? "light" : "dark";
  }
  try {
    Components.utils.import("resource://gre/modules/Services.jsm");
    document.documentElement.setAttribute("data-theme", resolve());
  } catch (e) {
    document.documentElement.setAttribute("data-theme", "dark");
  }
})();
