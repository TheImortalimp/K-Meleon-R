// Minimal stand-ins for the XPCOM objects the K-Meleon start page script expects.
(function () {
    var P = "kmr.";
    var prefs = {
        getCharPref: function (k, d) {
            var v = localStorage.getItem(P + k);
            if (v === null) {
                if (d !== undefined) return d;
                throw new Error("no pref " + k);
            }
            return v;
        },
        setCharPref: function (k, v) { localStorage.setItem(P + k, String(v)); },
        getComplexValue: function () { throw new Error("unsupported"); },
        addObserver: function () {},
        removeObserver: function () {}
    };
    window.Services = { prefs: prefs };
    var clipboard = {
        copyString: function (s) {
            if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(s);
        }
    };
    var Cu = { import: function () {} };
    window.Components = {
        utils: Cu,
        interfaces: { nsIPrefLocalizedString: {}, nsIClipboardHelper: {} },
        classes: { "@mozilla.org/widget/clipboardhelper;1": { getService: function () { return clipboard; } } }
    };
})();
