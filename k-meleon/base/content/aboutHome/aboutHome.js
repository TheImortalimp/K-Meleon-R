const Cu = Components.utils;
const Ci = Components.interfaces;
Cu.import("resource://gre/modules/Services.jsm");
Cu.import("resource://gre/modules/XPCOMUtils.jsm");

function setEngine() {
    let searchEngineName;
    try {
      searchEngineName = Services.prefs.getComplexValue("kmeleon.general.searchEngineName", Ci.nsIPrefLocalizedString);
    } catch (ex) {
      searchEngineName = Services.prefs.getCharPref("kmeleon.general.searchEngineName", '');
    }
    
	  //var searchEngineName = Services.search.defaultEngine.name;
	  let searchText = document.getElementById("searchText");	  
    searchText.addEventListener("blur", function searchText_onBlur() {
        searchText.removeEventListener("blur", searchText_onBlur);
        searchText.removeAttribute("autofocus");
    });
    
    searchText.placeholder = searchEngineName;
    /*var submission = engine.getSubmission("_searchTerms_", null, "homepage");
    var searchURL = submission.uri.spec;//Services.prefs.getComplexValue("kmeleon.general.searchEngine", Ci.nsIPrefLocalizedString);
    searchText.setAttribute("data-url", searchURL);
    */
}

window.addEventListener('load', function () {
    Services.prefs.addObserver("kmeleon.general.searchEngineName", setEngine, false);
    setEngine();
});

window.addEventListener('beforeunload', function () {
    Services.prefs.removeObserver("kmeleon.general.searchEngineName", setEngine);
});
	
const SEARCH_FALLBACK = "https://www.bing.com/search?q=";

function getSearchURL() {
    // The search service may be missing in this build, so fall back to the prefs.
    try {
        let submission = Services.search.defaultEngine.getSubmission("_searchTerms_", null, "homepage");
        if (submission && submission.uri) return submission.uri.spec;
    } catch (ex) {}
    try {
        return Services.prefs.getComplexValue("kmeleon.general.searchEngine", Ci.nsIPrefLocalizedString).data + "_searchTerms_";
    } catch (ex) {}
    try {
        return Services.prefs.getCharPref("kmeleon.general.searchEngine") + "_searchTerms_";
    } catch (ex) {}
    return SEARCH_FALLBACK + "_searchTerms_";
}

function onSearchSubmit(aEvent)
{
    aEvent.preventDefault();
    var searchURL = getSearchURL();
    var searchTerms = document.getElementById("searchText").value;
   
    if (searchURL && searchTerms.length > 0) {

        const SEARCH_TOKEN = "_searchTerms_";
        let searchPostData = document.documentElement.getAttribute("searchEnginePostData");
        if (searchPostData) {
            // Check if a post form already exists. If so, remove it.
            const POST_FORM_NAME = "searchFormPost";
            let form = document.forms[POST_FORM_NAME];
            if (form) {
                form.parentNode.removeChild(form);
            }

            // Create a new post form.
            form = document.body.appendChild(document.createElement("form"));
            form.setAttribute("name", POST_FORM_NAME);
            // Set the URL to submit the form to.
            form.setAttribute("action", searchURL.replace(SEARCH_TOKEN, searchTerms));
            form.setAttribute("method", "post");

            // Create new <input type=hidden> elements for search param.
            searchPostData = searchPostData.split("&");
            for (let postVar of searchPostData) {
                let [name, value] = postVar.split("=");
                if (value == SEARCH_TOKEN) {
                    value = searchTerms;
                }
                let input = document.createElement("input");
                input.setAttribute("type", "hidden");
                input.setAttribute("name", name);
                input.setAttribute("value", value);
                form.appendChild(input);
            }
            // Submit the form.
            form.submit();
        } else {
            searchURL = searchURL.replace(SEARCH_TOKEN, encodeURIComponent(searchTerms));
            window.location.href = searchURL;
        }
    }
}

/* ---- K-Meleon-R speed dial ---- */
const DIAL_PREF = "kmeleon.browser.speeddial2";
const DIAL_DEFAULTS = [
    { title: "MSN", url: "https://www.msn.com/en-gb" },
    { title: "Google", url: "https://www.google.com/" },
    { title: "YouTube", url: "https://www.youtube.com/" },
    { title: "Bandcamp", url: "https://bandcamp.com/" },
    { title: "Spotify", url: "https://open.spotify.com/" }
];
const THUMB_SIZE = 128;
let pendingImage = "";
let editIndex = -1;

function loadDial() {
    try {
        let list = JSON.parse(Services.prefs.getCharPref(DIAL_PREF));
        if (Array.isArray(list)) return list;
    } catch (ex) {}
    return DIAL_DEFAULTS.slice();
}

function saveDial(list) {
    Services.prefs.setCharPref(DIAL_PREF, JSON.stringify(list));
}

function isAllowedDialUrl(url) {
    return /^(https?:\/\/|windowsdefender:\/\/)/i.test(url);
}

function iconClass(url) {
    let host = "";
    try { host = new URL(url).hostname; } catch (ex) {}
    if (/wikipedia\.org$/i.test(host)) return "icon-wikipedia";
    if (/github\.com$/i.test(host)) return "icon-github";
    if (/(youtube\.com|youtu\.be)$/i.test(host)) return "icon-youtube";
    if (/reddit\.com$/i.test(host)) return "icon-reddit";
    if (/^windowsdefender:/i.test(url)) return "icon-shield";
    return "";
}

function isImageData(s) {
    return typeof s == "string" && /^data:image\/(png|jpeg|gif|webp);base64,[A-Za-z0-9+\/=]+$/.test(s);
}

// Shrinks a picked image file to a small PNG data URL so it fits in a pref.
function readThumbnail(file, done) {
    let reader = new FileReader();
    reader.onload = function () {
        let img = new Image();
        img.onload = function () {
            let canvas = document.createElement("canvas");
            canvas.width = canvas.height = THUMB_SIZE;
            let ctx = canvas.getContext("2d");
            let scale = Math.min(THUMB_SIZE / img.width, THUMB_SIZE / img.height);
            let w = img.width * scale, h = img.height * scale;
            ctx.drawImage(img, (THUMB_SIZE - w) / 2, (THUMB_SIZE - h) / 2, w, h);
            done(canvas.toDataURL("image/png"));
        };
        img.onerror = function () { done(""); };
        img.src = reader.result;
    };
    reader.onerror = function () { done(""); };
    reader.readAsDataURL(file);
}

// Uses the site's own logo (favicon); falls back to a letter when unavailable.
function applySiteLogo(icon, url, title) {
    let origin = "";
    try { origin = new URL(url).origin; } catch (ex) {}
    let letter = function () {
        icon.className += " letter";
        icon.textContent = (title || "?").charAt(0).toUpperCase();
    };
    if (!/^https?:/i.test(origin)) { letter(); return; }
    let probe = new Image();
    probe.onload = function () {
        if (probe.width < 2) { letter(); return; }
        icon.className += " fav";
        icon.style.backgroundImage = 'url("' + origin + '/favicon.ico")';
    };
    probe.onerror = letter;
    probe.src = origin + "/favicon.ico";
}

function makeTile(cls, label, iconClassName) {
    let tile = document.createElement("div");
    tile.className = "tile " + cls;
    let icon = document.createElement("span");
    icon.className = "icon " + (iconClassName || "");
    let name = document.createElement("span");
    name.className = "name";
    name.textContent = label;
    tile.appendChild(icon);
    tile.appendChild(name);
    return tile;
}

function renderDial() {
    let root = document.getElementById("speedDial");
    while (root.firstChild) root.removeChild(root.firstChild);
    let list = loadDial();

    list.forEach(function (entry, i) {
        if (!entry || !isAllowedDialUrl(String(entry.url))) return;
        let title = String(entry.title || entry.url);
        let known = iconClass(String(entry.url));
        let custom = isImageData(entry.img);
        let tile = makeTile("site", title, custom ? "custom" : known);
        let iconEl = tile.firstChild;
        if (custom) iconEl.style.backgroundImage = 'url("' + entry.img + '")';
        else if (!known) applySiteLogo(iconEl, String(entry.url), title);
        tile.title = entry.url;
        let edit = document.createElement("span");
        edit.className = "edit";
        edit.textContent = "\u270e";
        edit.title = "Change name, address or picture";
        edit.addEventListener("click", function (e) {
            e.stopPropagation();
            openDialForm(i);
        });
        tile.appendChild(edit);
        tile.addEventListener("click", function () {
            window.location.href = entry.url;
        });
        let remove = document.createElement("span");
        remove.className = "remove";
        remove.textContent = "\u00d7";
        remove.title = "Remove";
        remove.addEventListener("click", function (e) {
            e.stopPropagation();
            list.splice(i, 1);
            saveDial(list);
            renderDial();
        });
        tile.appendChild(remove);
        root.appendChild(tile);
    });

    let add = makeTile("add", "Add site", "");
    add.addEventListener("click", function () { openDialForm(-1); });
    root.appendChild(add);
}

function openDialForm(index) {
    editIndex = index;
    pendingImage = "";
    let entry = index >= 0 ? loadDial()[index] : null;
    document.getElementById("dialTitle").value = entry ? entry.title || "" : "";
    document.getElementById("dialUrl").value = entry ? entry.url || "" : "";
    document.getElementById("dialImage").value = "";
    document.getElementById("dialImageStatus").textContent = entry && isImageData(entry.img) ? "Custom picture set" : "";
    document.getElementById("dialClearImage").className = entry && isImageData(entry.img) ? "" : "hidden";
    document.getElementById("dialForm").className = "";
    document.getElementById("dialTitle").focus();
}

function onDialSave(aEvent) {
    aEvent.preventDefault();
    let url = document.getElementById("dialUrl").value.trim();
    if (!/^[a-z][a-z0-9+.-]*:/i.test(url)) url = "https://" + url;
    if (!/^https?:\/\//i.test(url)) return;
    let title = document.getElementById("dialTitle").value.trim() || url.replace(/^https?:\/\//i, "");
    let list = loadDial();
    let entry = { title: title, url: url };
    if (editIndex >= 0 && list[editIndex]) {
        if (isImageData(list[editIndex].img)) entry.img = list[editIndex].img;
        list[editIndex] = entry;
    } else {
        list.push(entry);
    }
    if (pendingImage === null) delete entry.img;
    else if (pendingImage) entry.img = pendingImage;
    saveDial(list);
    onDialCancel();
    renderDial();
}

function onDialCancel() {
    document.getElementById("dialTitle").value = "";
    document.getElementById("dialUrl").value = "";
    document.getElementById("dialImage").value = "";
    document.getElementById("dialImageStatus").textContent = "";
    document.getElementById("dialForm").className = "hidden";
    pendingImage = "";
    editIndex = -1;
}

window.addEventListener("load", function () {
    document.getElementById("dialCancel").addEventListener("click", onDialCancel);
    document.getElementById("dialImage").addEventListener("change", function () {
        let file = this.files && this.files[0];
        if (!file) return;
        readThumbnail(file, function (data) {
            pendingImage = data;
            document.getElementById("dialImageStatus").textContent = data ? "Picture ready" : "Could not read that image";
        });
    });
    document.getElementById("dialClearImage").addEventListener("click", function () {
        pendingImage = null;
        document.getElementById("dialImageStatus").textContent = "Picture will be removed";
    });
    // Gemini has no documented URL for prefilling a prompt, so just open it.
    document.getElementById("askGemini").addEventListener("click", function () {
        window.location.href = "https://gemini.google.com/app";
    });
    renderDial();
});


// ---- Theme toggle: dark (default) / light / system ----
var THEME_PREF = "kmeleon.display.theme";
var THEME_EFFECTIVE = "kmeleon.theme.effective";
var themeObserver = {
    observe: function () { applyTheme(); }
};

function getThemeMode() {
    try {
        let m = Services.prefs.getCharPref(THEME_PREF);
        if (m == "light" || m == "system") return m;
    } catch (e) {}
    return "dark";
}

function applyTheme() {
    let mode = getThemeMode();
    let eff = mode;
    if (mode == "system") {
        try { eff = Services.prefs.getCharPref(THEME_EFFECTIVE); } catch (e) { eff = "dark"; }
    }
    document.documentElement.setAttribute("data-theme", eff == "light" ? "light" : "dark");
    let btn = document.getElementById("themeToggle");
    if (btn) {
        btn.value = "Theme: " + (mode == "system" ? "System" : mode == "light" ? "Light" : "Dark");
    }
}

window.addEventListener("load", function () {
    applyTheme();
    try {
        Services.prefs.addObserver(THEME_PREF, themeObserver, false);
        Services.prefs.addObserver(THEME_EFFECTIVE, themeObserver, false);
    } catch (e) {}
    document.getElementById("themeToggle").addEventListener("click", function () {
        let next = { dark: "light", light: "system", system: "dark" }[getThemeMode()];
        Services.prefs.setCharPref(THEME_PREF, next);
        applyTheme();
    });
});

window.addEventListener("beforeunload", function () {
    try {
        Services.prefs.removeObserver(THEME_PREF, themeObserver);
        Services.prefs.removeObserver(THEME_EFFECTIVE, themeObserver);
    } catch (e) {}
});
