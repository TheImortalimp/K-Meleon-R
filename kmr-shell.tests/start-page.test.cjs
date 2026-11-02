const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const vm = require("node:vm");

const startDir = path.join(__dirname, "..", "kmr-shell", "start");
const destinations = {
    bing: ["www.bing.com", "/search"],
    google: ["www.google.com", "/search"],
    copilot: ["www.bing.com", "/copilotsearch"],
    gemini: ["www.google.com", "/search"]
};

function startPage(storage = new Map()) {
    const elements = new Map();
    const listeners = new Map();
    const navigations = [];
    const messages = [];
    const errors = [];
    function element(id) {
        if (!elements.has(id)) elements.set(id, {
            value: "", events: new Map(),
            addEventListener(type, callback) { this.events.set(type, callback); },
            removeEventListener() {},
            removeAttribute() {}
        });
        return elements.get(id);
    }
    const context = vm.createContext({
        console: { error: (...args) => errors.push(args) },
        document: {
            getElementById: element,
            documentElement: { setAttribute() {} }
        },
        localStorage: {
            getItem: key => storage.has(key) ? storage.get(key) : null,
            setItem: (key, value) => storage.set(key, String(value))
        },
        navigator: { clipboard: { writeText() { throw new Error("Queries must not use the clipboard"); } } },
        chrome: { webview: { postMessage: message => messages.push(message) } },
        location: { set href(url) { navigations.push(new URL(url)); } },
        matchMedia: () => ({ matches: false, addEventListener() {} }),
        addEventListener(type, callback) {
            if (!listeners.has(type)) listeners.set(type, []);
            listeners.get(type).push(callback);
        }
    });
    context.window = context;
    for (const file of ["shim.js", "aboutHome.js"])
        vm.runInContext(fs.readFileSync(path.join(startDir, file), "utf8"), context, { filename: file });
    vm.runInContext("renderDial = function () {};", context);
    for (const callback of listeners.get("load")) callback();
    return {
        context, element, navigations, messages, errors,
        click: id => element(id).events.get("click")(),
        submit(text) {
            element("searchText").value = text;
            context.onSearchSubmit({ preventDefault() {} });
        }
    };
}

for (const provider of Object.keys(destinations)) {
    test(`${provider}: full queries go only to the selected destination`, () => {
        for (const query of [
            "foo.bar", "explain https://example.com/a?x=1&y=2",
            "C# & C++ 100% #1 ? x=y+z", "caf\u00e9 \u65e5\u672c\u8a9e \u{1f30d}",
            "x".repeat(4096) + " full query"
        ]) {
            const page = startPage();
            page.context.kmrSetProvider(provider);
            page.submit("  " + query + "  ");
            assert.equal(page.navigations.length, 1);
            const url = page.navigations[0];
            assert.deepEqual([url.hostname, url.pathname], destinations[provider]);
            assert.equal(url.searchParams.get("q"), query);
            assert.equal(url.searchParams.get("udm"), provider === "gemini" ? "50" : null);
            assert.deepEqual(page.messages, []);
        }
    });
}

test("provider selection syncs labels and persistence without echoing native messages", () => {
    const storage = new Map();
    const page = startPage(storage);
    page.context.kmrSetProvider("gemini");
    assert.equal(page.element("searchProvider").value, "Search: Gemini");
    assert.equal(page.element("searchText").placeholder, "Gemini");
    assert.deepEqual(page.messages, []);
    page.click("searchProvider");
    assert.equal(page.element("searchProvider").value, "Search: Bing");
    assert.deepEqual(page.messages, ["provider:bing"]);
    page.click("searchProvider");
    page.click("searchProvider");
    assert.equal(page.element("searchProvider").value, "Search: Copilot");
    assert.equal(startPage(storage).element("searchProvider").value, "Search: Copilot");
});

test("Ask buttons explicitly override the selection for this query only", () => {
    for (const [button, provider] of [["askCopilot", "copilot"], ["askGemini", "gemini"]]) {
        const page = startPage();
        page.context.kmrSetProvider("google");
        const query = "https://example.com/a?x=1&y=2";
        page.element("searchText").value = query;
        page.click(button);
        assert.equal(page.navigations.length, 1);
        assert.deepEqual([page.navigations[0].hostname, page.navigations[0].pathname], destinations[provider]);
        assert.equal(page.navigations[0].searchParams.get("q"), query);
        assert.equal(page.element("searchProvider").value, "Search: Google");
        assert.deepEqual(page.messages, []);
    }
});

test("blank search is a no-op and empty Ask buttons open the intended home page", () => {
    const page = startPage();
    page.submit(" \t ");
    assert.deepEqual(page.navigations, []);
    page.click("askGemini");
    page.click("askCopilot");
    assert.deepEqual(page.navigations.map(url => url.href), [
        "https://gemini.google.com/app", "https://www.bing.com/copilotsearch"
    ]);
});

test("typing alone does not share input or navigate", () => {
    const page = startPage();
    page.element("searchText").value = "unsubmitted draft";
    assert.equal(page.element("searchText").events.has("input"), false);
    assert.deepEqual(page.navigations, []);
    assert.deepEqual(page.messages, []);
});

test("unknown native provider names are reported and cannot change the selection", () => {
    const page = startPage();
    page.context.kmrSetProvider("gemini");
    page.context.kmrSetProvider("https://untrusted.example");
    assert.equal(page.element("searchProvider").value, "Search: Gemini");
    assert.equal(page.errors.length, 1);
    assert.deepEqual(page.navigations, []);
});

test("the search field no longer limits input to 256 characters", () => {
    const html = fs.readFileSync(path.join(startDir, "index.html"), "utf8");
    const input = html.match(/<input[^>]*id="searchText"[^>]*>/)[0];
    assert.doesNotMatch(input, /maxlength=/);
});
