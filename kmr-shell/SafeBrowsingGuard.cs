using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace KMeleonR;

// Google Safe Browsing v4 lookups (the same blocklist Chrome and Gemini-era Google services use).
// Becomes active when an API key is present, either in settings.json ("gsbApiKey") or in the
// KMR_GSB_API_KEY environment variable; without a key it is silent and SmartScreen still covers
// reputation. Verdicts are cached, flagged navigations are cancelled and replaced by a local
// warning page. Lookups send only the URL being visited to Google, nothing else.
internal static class SafeBrowsingGuard
{
    private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(4) };
    private sealed record Verdict(string Threat, DateTime At);
    private static readonly ConcurrentDictionary<string, Verdict> cache = new();
    private static readonly TimeSpan CleanTtl = TimeSpan.FromMinutes(30);
    private static string apiKey = "";

    public static void Init() =>
        apiKey = Settings.Get("gsbApiKey", Environment.GetEnvironmentVariable("KMR_GSB_API_KEY") ?? "").Trim();

    public static bool Enabled => apiKey.Length > 0;

    public static void Attach(CoreWebView2 core)
    {
        if (!Enabled) return;
        var ui = SynchronizationContext.Current;
        core.NavigationStarting += (_, e) =>
        {
            var url = Canonical(e.Uri);
            if (url == null) return;
            if (cache.TryGetValue(url, out var v) && !Expired(v))
            {
                if (v.Threat != null)
                {
                    e.Cancel = true;
                    // A new navigation cannot start inside NavigationStarting; post it.
                    var flagged = e.Uri;
                    ui?.Post(_ => ShowWarning(core, flagged, v.Threat), null);
                }
                return;
            }
            var navigated = e.Uri;
            _ = Task.Run(async () =>
            {
                var threat = await Lookup(url);
                cache[url] = new Verdict(threat, DateTime.UtcNow);
                if (threat == null) return;
                // The page already started loading; stop it and swap in the warning.
                ui?.Post(_ =>
                {
                    try
                    {
                        if (core.Source == navigated || Canonical(core.Source) == url)
                        {
                            core.Stop();
                            ShowWarning(core, navigated, threat);
                        }
                    }
                    catch { }
                }, null);
            });
        };
    }

    private static bool Expired(Verdict v) => v.Threat == null && DateTime.UtcNow - v.At > CleanTtl;

    private static string Canonical(string raw)
    {
        try
        {
            var u = new Uri(raw);
            if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps) return null;
            if (u.Host is "kmr.start") return null;
            return u.GetLeftPart(UriPartial.Query);
        }
        catch { return null; }
    }

    private static async Task<string> Lookup(string url)
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                client = new { clientId = "k-meleon-r", clientVersion = "8.6.0" },
                threatInfo = new
                {
                    threatTypes = new[] { "MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION" },
                    platformTypes = new[] { "ANY_PLATFORM" },
                    threatEntryTypes = new[] { "URL" },
                    threatEntries = new[] { new { url } },
                },
            });
            using var resp = await http.PostAsync(
                "https://safebrowsing.googleapis.com/v4/threatMatches:find?key=" + Uri.EscapeDataString(apiKey),
                new StringContent(body, Encoding.UTF8, "application/json"));
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("matches", out var matches) && matches.GetArrayLength() > 0)
                return matches[0].TryGetProperty("threatType", out var t) ? t.GetString() : "THREAT";
            return null;
        }
        catch { return null; }   // network/API failure never blocks browsing
    }

    private static void ShowWarning(CoreWebView2 core, string url, string threat)
    {
        var safeUrl = WebUtility.HtmlEncode(url ?? "");
        var safeThreat = WebUtility.HtmlEncode(threat switch
        {
            "SOCIAL_ENGINEERING" => "a phishing / social-engineering page",
            "MALWARE" => "a malware page",
            "UNWANTED_SOFTWARE" => "a page distributing unwanted software",
            "POTENTIALLY_HARMFUL_APPLICATION" => "a page distributing potentially harmful applications",
            _ => "a dangerous page (" + threat + ")",
        });
        try
        {
            DefenderGuard.AllowNextDataNavigation(core);
            core.NavigateToString($$"""
<!doctype html><html><head><meta charset="utf-8"><title>Dangerous site blocked</title>
<style>body{background:#1a0a0d;color:#f2d7dc;font:15px/1.6 'Segoe UI',sans-serif;display:flex;align-items:center;justify-content:center;min-height:95vh;margin:0}
main{max-width:620px;padding:32px;border:1px solid #5c1f2b;border-radius:12px;background:#24101a}
h1{color:#ff4d6d;font-size:22px}code{word-break:break-all;color:#ffb3c0}</style></head>
<body><main><h1>&#9888; Dangerous site blocked</h1>
<p>Google Safe Browsing reports this address as {{safeThreat}}:</p>
<p><code>{{safeUrl}}</code></p>
<p>K-Meleon-R stopped the page before it could load. Use the Back button to leave.</p>
</main></body></html>
""");
        }
        catch { }
    }
}
