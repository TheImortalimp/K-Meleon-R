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
  "proposal-async-generator-functions", "transform-async-to-generator",
  "transform-classes", "transform-block-scoping", "transform-new-target",
  "transform-unicode-regex", "transform-exponentiation-operator", "transform-object-super",
  "transform-parameters", "transform-destructuring"
];

// Minimal System.register loader that runs in the page. It executes ES
// modules after they were converted to System.register format.
function loaderMain(g) {
  if (g.System) return;
  var reg = {}, pending = null;
  function resolve(s, base) {
    try { return new URL(s, base || document.baseURI).href; } catch (e) { return s; }
  }
  function instantiate(url, r, p) {
    var deps = p[0], mod;
    function exp(n, v) {
      if (typeof n == "object") { for (var k in n) r.ns[k] = n[k]; }
      else r.ns[n] = v;
      for (var i = 0; i < r.importers.length; i++) r.importers[i](r.ns);
      return v;
    }
    mod = p[1](exp, {
      id: url,
      meta: { url: url },
      "import": function (s) { return load(resolve(s, url)); }
    });
    return Promise.all(deps.map(function (d, i) {
      var du = resolve(d, url);
      return load(du).then(function () {
        var dr = reg[du];
        dr.importers.push(mod.setters[i]);
        mod.setters[i](dr.ns);
      });
    })).then(function () {
      return mod.execute && mod.execute();
    }).then(function () { return r.ns; });
  }
  function load(url) {
    var r = reg[url];
    if (r) return r.p;
    r = reg[url] = { ns: {}, importers: [], p: null };
    r.p = new Promise(function (ok, fail) {
      var sc = document.createElement("script");
      sc.async = true;
      if (g.__kmNonce) sc.setAttribute("nonce", g.__kmNonce);
      sc.onload = function () {
        var p = pending; pending = null;
        if (!p) { ok(r.ns); return; }
        instantiate(url, r, p).then(ok, function (e) { setTimeout(function () { throw e; }); fail(e); });
      };
      sc.onerror = function () { fail(new Error("Failed to load module " + url)); };
      sc.src = url;
      (document.head || document.documentElement).appendChild(sc);
    });
    return r.p;
  }
  g.System = {
    register: function (d, f) { pending = [d, f]; },
    "import": function (s, base) { return load(resolve(s, base)); },
    runInline: function (id) {
      var p = pending; pending = null;
      var r = reg[id] = { ns: {}, importers: [], p: null };
      r.p = instantiate(id, r, p);
      return r.p;
    }
  };
}
const LOADER = "(" + loaderMain.toString() + ")(window);";

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
      "function __kmTransform(src, mod) {" +
      "  return Babel.transform(src, {plugins: mod ? __kmPlugins.concat(['transform-modules-systemjs']) : __kmPlugins," +
      "    compact: false, comments: false," +
      "    sourceType: mod ? 'module' : 'script', parserOpts: {allowReturnOutsideFunction: !mod}}).code;" +
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

function wrapDbg(out, label) {
  if (!debugProbe() || /^\(function loaderMain/.test(out)) return out;
  return "try{\n" + out + "\n}catch(e){try{new Image().src=\"http://localhost:8765/b?\"+encodeURIComponent(\"BUNDLE \"+" +
         JSON.stringify(label.slice(-80)) + "+\" \"+(e&&e.stack||e))}catch(x){}throw e}";
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
  if (cached) return wrapDbg(cached, label);

  let t0 = Date.now();
  try {
    let out;
    try {
      out = sb.__kmTransform(src, false);
    } catch (e) {
      if (!/import|export|sourceType/.test(String(e))) throw e;
      out = LOADER + sb.__kmTransform(src, true);
    }
    if (/\bimport\s*\(/.test(out) && !/^\(function loaderMain/.test(out)) {
      let fn = "__kmI" + key.slice(0, 8);
      let base = /#inline$/.test(label) ? "document.baseURI" : JSON.stringify(label.replace(/#inline$/, ""));
      out = LOADER + "var " + fn + "=function(s){return System.import(s," + base + ")};" +
            out.replace(/\bimport\s*\(/g, fn + "(");
    }
    log("converted " + label + " " + src.length + " -> " + out.length + " in " + (Date.now() - t0) + "ms");
    writeCache(key, out);
    return wrapDbg(out, label);
  } catch (e) {
    log("convert failed " + label + ": " + String(e).slice(0, 200));
    return src;
  }
}

const INLINE_SCRIPT = /(<script\b)([^>]*)(>)([\s\S]*?)(<\/script\s*>)/gi;
const HEAD_OPEN = /<head\b[^>]*>/i;

function compatHtml(html, label) {
  let useModules = !/<script\b[^>]*\bnomodule\b/i.test(html);
  let nonce = null, needLoader = false;
  let out = html.replace(INLINE_SCRIPT, function (m, open, attrs, gt, body, close) {
    let t = /\btype\s*=\s*["']?([^"'\s>]*)/i.exec(attrs);
    let type = t ? t[1] : "";
    let n = /\bnonce\s*=\s*["']?([^"'\s>]*)/i.exec(attrs);
    if (/^module$/i.test(type)) {
      if (!useModules) return m;
      if (n && !nonce) nonce = n[1];
      let s = /\bsrc\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+))/i.exec(attrs);
      let nattr = n ? ' nonce="' + n[1] + '"' : "";
      needLoader = true;
      if (s) {
        let url = (s[1] || s[2] || s[3]).replace(/&amp;/g, "&");
        return "<script" + nattr + ">System.import(" + JSON.stringify(url) + ").catch(function(e){setTimeout(function(){throw e})});" + close;
      }
      if (!body.trim()) return m;
      let sb = getBabelSandbox();
      if (!sb) return m;
      try {
        return "<script" + nattr + ">" + sb.__kmTransform(body, true) + "\nSystem.runInline(document.baseURI+\"#m" +
               sha1Hex(body).slice(0, 8) + "\");" + close;
      } catch (e) {
        log("inline module failed " + label + ": " + String(e).slice(0, 200));
        return m;
      }
    }
    if (/\bsrc\s*=/i.test(attrs) || !body.trim()) return m;
    if (!/^(text|application)\/(x-)?(java|ecma)script$|^$/i.test(type)) return m;
    let res = compat(body, label + "#inline");
    return res === body ? m : open + attrs + gt + res + close;
  });
  if (needLoader) {
    let tag = "<script" + (nonce ? ' nonce="' + nonce + '"' : "") + ">" +
              (nonce ? "window.__kmNonce=" + JSON.stringify(nonce) + ";" : "") + LOADER + "</script>";
    let h = HEAD_OPEN.exec(out);
    out = h ? out.slice(0, h.index + h[0].length) + tag + out.slice(h.index + h[0].length) : tag + out;
  }
  if (debugProbe()) {
    out = out.replace(/<script\b(?![^>]*crossorigin)([^>]*\bsrc=)/gi, '<script crossorigin="anonymous"$1');
    let nn = nonce || (/\bnonce\s*=\s*["']([^"']+)/i.exec(out) || [])[1];
    let tag = "<script" + (nn ? ' nonce="' + nn + '"' : "") + ">(" + probeMain.toString() + ")();</script>";
    let h = HEAD_OPEN.exec(out);
    out = h ? out.slice(0, h.index + h[0].length) + tag + out.slice(h.index + h[0].length) : tag + out;
  }
  return out;
}

// Test-only: reports page errors to a local server when kmeleon.jscompat.probe is set.
function debugProbe() {
  try { return Services.prefs.getBoolPref("kmeleon.jscompat.probe"); } catch (e) { return false; }
}
function probeMain() {
  function s(m) {
    try { new Image().src = "http://localhost:8765/b?" + encodeURIComponent(location.host + " " + m).slice(0, 1500); } catch (e) {}
  }
  s("PROBE start");
  window.addEventListener("error", function (e) { s("ERR " + e.message + " @" + e.filename + ":" + e.lineno); }, true);
  window.addEventListener("unhandledrejection", function (e) {
    s("REJ " + (e.reason && (e.reason.stack || e.reason.message) || e.reason));
  });
  ["error", "warn"].forEach(function (k) {
    var o = console[k];
    console[k] = function () {
      s("C." + k + " " + [].slice.call(arguments).join(" ").slice(0, 300));
      try { return o.apply(console, arguments); } catch (e) {}
    };
  });
  setTimeout(function () {
    s("DOM kids=" + document.body.children.length + " len=" + document.body.innerHTML.length);
  }, 15000);
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
      if (debugProbe()) ch.setResponseHeader("Access-Control-Allow-Origin", "*", false);
      if (debugProbe()) ch.setResponseHeader("Access-Control-Allow-Origin", "*", false);
      let enc = /charset=([^;\s]+)/i.exec(type);
      if (enc && !/^utf-?8$/i.test(enc[1])) return;
      let tc = ch.QueryInterface(Ci.nsITraceableChannel);
      let tee = new TeeListener(null, kind, ch.URI.spec);
      tee.orig = tc.setNewListener(tee);
    } catch (e) {
      log("hook error: " + e);
    }
  }
};

var NSGetFactory = XPCOMUtils.generateNSGetFactory([kmJsCompat]);
