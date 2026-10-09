"use strict";

// Rewrites modern JavaScript syntax (async/await, ?., ??, object spread,
// class fields, ...) into syntax this engine understands, so that current
// web apps (Google and others) can run.

const Cc = Components.classes;
const Ci = Components.interfaces;
const Cr = Components.results;
const Cu = Components.utils;
Cu.import("resource://gre/modules/XPCOMUtils.jsm");
Cu.import("resource://gre/modules/Services.jsm");
Cu.import("resource://gre/modules/FileUtils.jsm");

const PREF_ENABLED = "kmeleon.jscompat.enabled";
const PREF_LOG = "kmeleon.jscompat.log";
const MAX_SIZE = 12 * 1024 * 1024;
const TOPICS = ["http-on-examine-response", "http-on-examine-cached-response",
                "http-on-examine-merged-response"];

// Plugins keep to syntax and leave every API the engine already provides alone.
const BABEL_PLUGINS = [
  "proposal-class-properties", "proposal-private-methods",
  "proposal-optional-chaining", "proposal-nullish-coalescing-operator",
  "proposal-object-rest-spread", "proposal-logical-assignment-operators",
  "proposal-optional-catch-binding", "proposal-numeric-separator",
  "proposal-async-generator-functions", "transform-async-to-generator"
];

let gSandbox = null;
let gSandboxFailed = false;
let gCacheDir = null;

function enabled() {
  try { return Services.prefs.getBoolPref(PREF_ENABLED); } catch (e) { return true; }
}

function log(msg) {
  try {
    if (!Services.prefs.getBoolPref(PREF_LOG)) return;
  } catch (e) { return; }
  try {
    let f = getCacheDir().clone();
    f.append("log.txt");
    let s = FileUtils.openFileOutputStream(f, FileUtils.MODE_WRONLY | FileUtils.MODE_CREATE | FileUtils.MODE_APPEND);
    let line = new Date().toISOString() + " " + msg + "\n";
    s.write(line, line.length);
    s.close();
  } catch (e) {}
}

function getCacheDir() {
  if (!gCacheDir) {
    let d = Services.dirsvc.get("ProfD", Ci.nsIFile);
    d.append("jscompat");
    if (!d.exists()) d.create(Ci.nsIFile.DIRECTORY_TYPE, 0o755);
    gCacheDir = d;
  }
  return gCacheDir;
}

function getBabelSandbox() {
  if (gSandbox || gSandboxFailed) return gSandbox;
  try {
    let t0 = Date.now();
    let file = __LOCATION__.parent.clone();
    file.append("babel.min.js");
    let sb = Cu.Sandbox(Cc["@mozilla.org/systemprincipal;1"].createInstance(Ci.nsIPrincipal),
                        { wantComponents: false, sandboxName: "kmJsCompat" });
    Cu.evalInSandbox(
      "var self = this; var window = this;" +
      "var console = {log: function(){}, warn: function(){}, error: function(){}, info: function(){}, debug: function(){}, trace: function(){}};",
      sb);
    Services.scriptloader.loadSubScript(Services.io.newFileURI(file).spec, sb, "UTF-8");
    Cu.evalInSandbox(
      "var __kmPlugins = " + JSON.stringify(BABEL_PLUGINS) + ";" +
      "function __kmTransform(src) {" +
      "  return Babel.transform(src, {plugins: __kmPlugins, compact: false, comments: false," +
      "    sourceType: 'script', parserOpts: {allowReturnOutsideFunction: true}}).code;" +
      "}" +
      "function __kmParses(src) { try { new Function(src); return true; } catch (e) { return false; } }",
      sb);
    gSandbox = sb;
    log("babel loaded in " + (Date.now() - t0) + "ms");
  } catch (e) {
    gSandboxFailed = true;
    log("babel load failed v2: " + e + " " + String(e.stack).slice(0, 400));
    Cu.reportError(e);
  }
  return gSandbox;
}

function sha1Hex(str) {
  let ch = Cc["@mozilla.org/security/hash;1"].createInstance(Ci.nsICryptoHash);
  ch.init(ch.SHA1);
  let conv = Cc["@mozilla.org/intl/scriptableunicodeconverter"].createInstance(Ci.nsIScriptableUnicodeConverter);
  conv.charset = "UTF-8";
  let data = conv.convertToByteArray(str, {});
  ch.update(data, data.length);
  let bin = ch.finish(false);
  let hex = "";
  for (let i = 0; i < bin.length; i++) hex += ("0" + bin.charCodeAt(i).toString(16)).slice(-2);
  return hex;
}

function readCache(key) {
  try {
    let f = getCacheDir().clone();
    f.append(key + ".js");
    if (!f.exists()) return null;
    let s = Cc["@mozilla.org/network/file-input-stream;1"].createInstance(Ci.nsIFileInputStream);
    s.init(f, -1, 0, 0);
    let c = Cc["@mozilla.org/intl/converter-input-stream;1"].createInstance(Ci.nsIConverterInputStream);
    c.init(s, "UTF-8", 65536, 0);
    let out = "", o = {};
    while (c.readString(65536, o) != 0) out += o.value;
    c.close();
    return out;
  } catch (e) { return null; }
}

function writeCache(key, text) {
  try {
    let f = getCacheDir().clone();
    f.append(key + ".js");
    let s = FileUtils.openSafeFileOutputStream(f);
    let c = Cc["@mozilla.org/intl/converter-output-stream;1"].createInstance(Ci.nsIConverterOutputStream);
    c.init(s, "UTF-8", 0, 0);
    c.writeString(text);
    FileUtils.closeSafeFileOutputStream(s);
  } catch (e) { log("cache write failed: " + e); }
}

// Returns the original source when it already parses or cannot be converted.
function compat(src, label) {
  if (src.length > MAX_SIZE) return src;
  let sb = getBabelSandbox();
  if (!sb) return src;
  try {
    if (sb.__kmParses(src)) { log("parses ok " + label + " " + src.length); return src; }
  } catch (e) { return src; }

  let key = sha1Hex(src);
  let cached = readCache(key);
  if (cached) return cached;

  let t0 = Date.now();
  try {
    let out = sb.__kmTransform(src);
    log("converted " + label + " " + src.length + " -> " + out.length + " in " + (Date.now() - t0) + "ms");
    writeCache(key, out);
    return out;
  } catch (e) {
    log("convert failed " + label + ": " + String(e).slice(0, 200));
    return src;
  }
}

const INLINE_SCRIPT = /(<script\b)([^>]*)(>)([\s\S]*?)(<\/script\s*>)/gi;

function compatHtml(html, label) {
  return html.replace(INLINE_SCRIPT, function (m, open, attrs, gt, body, close) {
    if (/\bsrc\s*=/i.test(attrs) || !body.trim()) return m;
    let t = /\btype\s*=\s*["']?([^"'\s>]*)/i.exec(attrs);
    if (t && !/^(text|application)\/(x-)?(java|ecma)script$|^$/i.test(t[1])) return m;
    let out = compat(body, label + "#inline");
    return out === body ? m : open + attrs + gt + out + close;
  });
}

function TeeListener(orig, kind, label) {
  this.orig = orig;
  this.kind = kind;
  this.label = label;
  this.data = "";
  this.request = null;
  this.context = null;
}
TeeListener.prototype = {
  QueryInterface: XPCOMUtils.generateQI([Ci.nsIStreamListener, Ci.nsIRequestObserver]),

  onStartRequest: function (request, context) {
    this.request = request;
    this.context = context;
  },

  onDataAvailable: function (request, context, stream, offset, count) {
    let bis = Cc["@mozilla.org/binaryinputstream;1"].createInstance(Ci.nsIBinaryInputStream);
    bis.setInputStream(stream);
    this.data += bis.readBytes(count);
  },

  onStopRequest: function (request, context, status) {
    let out = this.data;
    if (Components.isSuccessCode(status) && out.length && out.length <= MAX_SIZE) {
      try {
        out = this.convert(out);
      } catch (e) {
        log("tee error " + this.label + ": " + e);
        out = this.data;
      }
    }
    this.orig.onStartRequest(this.request || request, this.context || context);
    if (out.length) {
      let sis = Cc["@mozilla.org/io/string-input-stream;1"].createInstance(Ci.nsIStringInputStream);
      sis.setData(out, out.length);
      this.orig.onDataAvailable(this.request || request, this.context || context, sis, 0, out.length);
    }
    this.orig.onStopRequest(this.request || request, this.context || context, status);
    this.data = "";
  },

  convert: function (bytes) {
    let conv = Cc["@mozilla.org/intl/scriptableunicodeconverter"].createInstance(Ci.nsIScriptableUnicodeConverter);
    conv.charset = "UTF-8";
    let text = conv.ConvertToUnicode(bytes);
    let res = this.kind == "html" ? compatHtml(text, this.label) : compat(text, this.label);
    if (res === text) return bytes;
    return conv.ConvertFromUnicode(res) + conv.Finish();
  }
};

function kmJsCompat() {}
kmJsCompat.prototype = {
  classID: Components.ID("{b1d0a0c5-6b7e-4f46-9d3e-3f6a2a7c9e11}"),
  QueryInterface: XPCOMUtils.generateQI([Ci.nsIObserver]),

  observe: function (subject, topic, data) {
    if (topic == "app-startup") {
      Services.obs.addObserver(this, "profile-after-change", false);
    } else if (topic == "profile-after-change") {
      Services.obs.removeObserver(this, "profile-after-change");
      for (let t of TOPICS) Services.obs.addObserver(this, t, false);
      Services.obs.addObserver(this, "quit-application", false);
      this.errorListener = {
        observe: function (msg) {
          try {
            let e = msg.QueryInterface(Ci.nsIScriptError);
            if (/^https?:/.test(e.sourceName || ""))
              log("PAGE " + (e.flags & 1 ? "warn " : "error ") + e.errorMessage + " @" + String(e.sourceName).slice(-60) + ":" + e.lineNumber);
          } catch (x) {}
        },
        QueryInterface: XPCOMUtils.generateQI([Ci.nsIConsoleListener])
      };
      Services.console.registerListener(this.errorListener);
    } else if (topic == "quit-application") {
      for (let t of TOPICS) Services.obs.removeObserver(this, t);
      Services.obs.removeObserver(this, "quit-application");
    } else if (TOPICS.indexOf(topic) >= 0) {
      this.onResponse(subject);
    }
  },

  onResponse: function (subject) {
    try {
      if (!enabled()) return;
      let ch = subject.QueryInterface(Ci.nsIHttpChannel);
      if (!/^https?$/.test(ch.URI.scheme)) return;
      if (ch.responseStatus != 200) return;
      let type = "";
      try { type = ch.getResponseHeader("Content-Type"); } catch (e) { return; }
      let kind = null;
      if (/(java|ecma)script/i.test(type)) kind = "js";
      else if (/text\/html/i.test(type)) kind = "html";
      if (!kind) return;
      let enc = /charset=([^;\s]+)/i.exec(type);
      if (enc && !/^utf-?8$/i.test(enc[1])) return;
      let tc = ch.QueryInterface(Ci.nsITraceableChannel);
      let tee = new TeeListener(null, kind, ch.URI.spec.slice(0, 120));
      tee.orig = tc.setNewListener(tee);
    } catch (e) {
      log("hook error: " + e);
    }
  }
};

var NSGetFactory = XPCOMUtils.generateNSGetFactory([kmJsCompat]);
