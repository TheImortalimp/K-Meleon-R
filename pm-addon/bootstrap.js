"use strict";

const Cc = Components.classes;
const Ci = Components.interfaces;
const Cu = Components.utils;
Cu.import("resource://gre/modules/Services.jsm");
Cu.import("resource://gre/modules/XPCOMUtils.jsm");

const CHROME_PAGE = "chrome://kmeleon/content/aboutHome/aboutHome.xhtml";
const START_PAGE = "about:kmr";
const CONFIGURED_PREF = "extensions.kmeleonr.configured";
const DEFAULTS = {
    "browser.startup.homepage": START_PAGE,
    "browser.newtab.url": START_PAGE,
    "browser.startup.page": 1
};

const ABOUT_CID = Components.ID("{6f1c3d0e-8e1b-4a47-9d5c-2b1f6a8c9e11}");
const ABOUT_CONTRACT = "@mozilla.org/network/protocol/about;1?what=kmr";

function AboutKmr() {}
AboutKmr.prototype = {
    classID: ABOUT_CID,
    QueryInterface: XPCOMUtils.generateQI([Ci.nsIAboutModule]),
    getURIFlags: function (aURI) {
        return Ci.nsIAboutModule.ALLOW_SCRIPT;
    },
    newChannel: function (aURI, aLoadInfo) {
        let channel = Services.io.newChannelFromURIWithLoadInfo(Services.io.newURI(CHROME_PAGE, null, null), aLoadInfo);
        channel.originalURI = aURI;
        return channel;
    }
};

let factory = {
    createInstance: function (aOuter, aIID) {
        if (aOuter) throw Components.results.NS_ERROR_NO_AGGREGATION;
        return new AboutKmr().QueryInterface(aIID);
    },
    QueryInterface: XPCOMUtils.generateQI([Ci.nsIFactory])
};

// Stored once as ordinary user values, because the first window of a launch is
// opened before add-ons start and can only see prefs saved in the profile.
function setDefaults() {
    if (Services.prefs.getBoolPref(CONFIGURED_PREF, false)) return;
    for (let name in DEFAULTS) {
        let v = DEFAULTS[name];
        if (typeof v == "number") Services.prefs.setIntPref(name, v);
        else Services.prefs.setCharPref(name, v);
    }
    Services.prefs.setBoolPref(CONFIGURED_PREF, true);
}
// On the very first launch the window opens before this add-on starts and
// ends up on a blank page, so the start page is loaded into it afterwards.
function fillBlankStartTab(win) {
    try {
        let gb = win.gBrowser;
        if (!gb || gb.tabs.length != 1) return;
        let b = gb.selectedBrowser;
        if (b.currentURI.spec == "about:blank") b.loadURI(START_PAGE);
    } catch (e) {}
}

let fillTimer = null;

function startup(data, reason) {
    let firstRun = !Services.prefs.getBoolPref(CONFIGURED_PREF, false);
    Components.manager.QueryInterface(Ci.nsIComponentRegistrar)
        .registerFactory(ABOUT_CID, "about:kmr", ABOUT_CONTRACT, factory);
    setDefaults();
    if (reason != APP_STARTUP || !firstRun) return;
    let tries = 0;
    fillTimer = Cc["@mozilla.org/timer;1"].createInstance(Ci.nsITimer);
    fillTimer.initWithCallback(function () {
        let wins = Services.wm.getEnumerator("navigator:browser");
        while (wins.hasMoreElements()) fillBlankStartTab(wins.getNext());
        if (++tries >= 16) fillTimer.cancel();
    }, 500, Ci.nsITimer.TYPE_REPEATING_SLACK);
}
function shutdown(data, reason) {
    if (reason == APP_SHUTDOWN) return;
    Components.manager.QueryInterface(Ci.nsIComponentRegistrar).unregisterFactory(ABOUT_CID, factory);
}

function install(data, reason) {}
function uninstall(data, reason) {}
