using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Runtime.InteropServices;
using System.Text.Json;
using static KMeleonR.SearchNavigation;

namespace KMeleonR;

internal sealed class BrowserTab
{
    public WebView2 View;
    public Button Header;
    public string Title = "New Tab";
    public bool IsPrivate;
}

internal sealed class MainForm : Form
{
    private readonly Panel tabStrip = new() { Dock = DockStyle.Top, Height = 34 };
    private readonly FlowLayoutPanel tabFlow = new() { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false };
    private readonly Button newTabButton = MakeButton("+", 30);
    private readonly Panel toolbar = new() { Dock = DockStyle.Top, Height = 40 };
    private readonly Button back = MakeButton("\u25C0", 34);
    private readonly Button fwd = MakeButton("\u25B6", 34);
    private readonly Button reload = MakeButton("\u21BB", 34);
    private readonly Button home = MakeButton("\u2302", 34);
    private readonly TextBox omnibox = new() { BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11f) };
    private readonly Button provider = MakeButton("", 84);
    private readonly Button askCopilot = MakeButton("Ask Copilot", 96);
    private readonly Button askGemini = MakeButton("Ask Gemini", 92);
    private readonly Button theme = MakeButton("", 92);
    private readonly Button shodan = MakeButton("SHODAN", 86);
    private readonly Panel host = new() { Dock = DockStyle.Fill };
    private readonly List<BrowserTab> tabs = new();
    private BrowserTab current;
    private CoreWebView2Environment env;
    private readonly string initialUrl;
    private int providerIndex;
    private string themeMode;

    // SHODAN side pane: a right-docked, resizable WebView2 that hosts the bundled SHODAN voice AI.
    // Modes (all coherent and exception-safe): minimize to a rail, expand width, fullscreen,
    // pop out into a floating window and dock back, and close (the live session is kept either way).
    private const int ShodanNormalWidth = 440;
    private const int ShodanExpandedWidth = 820;
    private const int ShodanRailWidth = 34;
    private readonly Splitter shodanSplit = new() { Dock = DockStyle.Right, Width = 4 };
    private readonly Panel shodanDock = new() { Dock = DockStyle.Right, Width = ShodanNormalWidth, Visible = false };
    private readonly Panel shodanInner = new() { Dock = DockStyle.Fill };
    private readonly Panel shodanHeader = new() { Dock = DockStyle.Top, Height = 30 };
    private readonly Panel shodanBody = new() { Dock = DockStyle.Fill };
    private readonly Label shodanTitle = new() { Text = "SHODAN", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
    private readonly Button shodanMinBtn = MakeButton("\u2013", 28);
    private readonly Button shodanExpandBtn = MakeButton("\u2194", 28);
    private readonly Button shodanFullBtn = MakeButton("\u26F6", 28);
    private readonly Button shodanFloatBtn = MakeButton("\u2197", 28);
    private readonly Button shodanCloseBtn = MakeButton("\u2715", 28);
    private readonly Button shodanRestoreRail = new() { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Text = "S\nH\nO\nD\nA\nN", Font = new Font("Segoe UI", 8f, FontStyle.Bold), TabStop = false, Visible = false };
    private WebView2 shodanView;
    private Form shodanFloat;
    private bool shodanReady;
    private bool shodanExpanded;
    private bool shodanMinimized;
    private bool shodanFullscreen;
    private FormWindowState shodanFloatState = FormWindowState.Normal;
    private FormBorderStyle shodanFloatBorder = FormBorderStyle.Sizable;

    public MainForm(string initialUrl)
    {
        this.initialUrl = initialUrl;
        Text = "K-Meleon-R";
        AutoScaleMode = AutoScaleMode.Dpi;
        Size = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        providerIndex = Math.Max(0, Array.FindIndex(Providers, p => p.Name == Settings.Get("provider", "Bing")));
        themeMode = Settings.Get("theme", "dark");

        tabStrip.Controls.Add(tabFlow);
        newTabButton.Dock = DockStyle.Right;
        tabStrip.Controls.Add(newTabButton);

        var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, Padding = new Padding(0, 4, 4, 0) };
        right.Controls.AddRange(new Control[] { provider, askCopilot, askGemini, theme, shodan });
        var left = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false, Padding = new Padding(4, 4, 0, 0) };
        left.Controls.AddRange(new Control[] { back, fwd, reload, home });
        omnibox.Dock = DockStyle.Fill;
        var omniHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 8, 6, 6) };
        omniHost.Controls.Add(omnibox);
        toolbar.Controls.Add(omniHost);
        toolbar.Controls.Add(right);
        toolbar.Controls.Add(left);

        BuildShodanPane();

        Controls.Add(host);
        Controls.Add(shodanSplit);
        Controls.Add(shodanDock);
        Controls.Add(toolbar);
        Controls.Add(tabStrip);

        back.Click += (_, _) => current?.View.CoreWebView2?.GoBack();
        fwd.Click += (_, _) => current?.View.CoreWebView2?.GoForward();
        reload.Click += (_, _) => current?.View.CoreWebView2?.Reload();
        home.Click += (_, _) => current?.View.CoreWebView2?.Navigate(StartUrl);
        newTabButton.Click += async (_, _) => await AddTab(StartUrl);
        omnibox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Navigate(omnibox.Text); current?.View.Focus(); }
        };
        omnibox.GotFocus += (_, _) => omnibox.SelectAll();
        provider.Click += (_, _) =>
        {
            SetProvider((providerIndex + 1) % Providers.Length);
        };
        askCopilot.Click += async (_, _) => await Ask("Copilot");
        askGemini.Click += async (_, _) => await Ask("Gemini");
        theme.Click += (_, _) => CycleTheme();
        shodan.Click += async (_, _) => await ToggleShodan();
        shodanMinBtn.Click += (_, _) => ToggleShodanMinimized();
        shodanExpandBtn.Click += (_, _) => SetShodanExpanded(!shodanExpanded);
        shodanFullBtn.Click += (_, _) => ToggleShodanFullscreen();
        shodanFloatBtn.Click += (_, _) => { if (shodanFloat != null) DockShodan(); else FloatShodan(); };
        shodanCloseBtn.Click += (_, _) => CloseShodan();
        shodanRestoreRail.Click += (_, _) => ToggleShodanMinimized();
        // Silent self-healing: the capture shield is re-applied on every guard refresh cycle.
        DefenderGuard.Reasserted += () => { try { CaptureGuard.Apply(this, ShieldOn); } catch { } };

        ApplyChrome();
        Shown += async (_, _) => await Start();
    }

    // Capture guard (anti-screencap) runs invisibly; it is on unless settings.json sets "shield" to "off".
    private bool ShieldOn => Settings.Get("shield", "on") == "on";

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CaptureGuard.Apply(this, ShieldOn);
    }

    private static Button MakeButton(string text, int width) => new()
    {
        Text = text,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        MinimumSize = new Size(width, 28),
        Padding = new Padding(6, 0, 6, 0),
        FlatStyle = FlatStyle.Flat,
        Margin = new Padding(2, 0, 2, 0),
        TabStop = false,
    };

    private async Task Start()
    {
        try
        {
            var dbg = Environment.GetEnvironmentVariable("KMR_DEBUG_PORT");
            var args = "--autoplay-policy=no-user-gesture-required" + (string.IsNullOrEmpty(dbg) ? "" : " --remote-debugging-port=" + dbg);
            var opts = new CoreWebView2EnvironmentOptions(args) { AllowSingleSignOnUsingOSPrimaryAccount = false };
            env = await CoreWebView2Environment.CreateAsync(null, Settings.ProfileDir, opts);
        }
        catch (Exception ex)
        {
            if (MessageBox.Show(this,
                    "K-Meleon-R needs the Microsoft Edge WebView2 Runtime, which was not found.\n\n" + ex.Message +
                    "\n\nOpen the download page now?", "K-Meleon-R", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "https://developer.microsoft.com/microsoft-edge/webview2/") { UseShellExecute = true });
            Close();
            return;
        }
        await AddTab(Resolve(initialUrl, Providers[providerIndex]) ?? StartUrl);
    }

    private static readonly string[] ThemeModes = { "dark", "light", "sand", "sky", "system" };

    private sealed record Palette(Color Bg, Color Panel, Color Field, Color Button, Color ButtonHover, Color TabActive, Color Fg, Color Accent);

    // The style actually drawn: "system" resolves to dark or the neutral light style.
    private string EffectiveStyle => themeMode == "system" ? (SystemIsLight() ? "light" : "dark") : themeMode;

    private bool IsLight => EffectiveStyle != "dark";

    private Palette CurrentPalette() => EffectiveStyle switch
    {
        "light" => new Palette(Color.FromArgb(244, 245, 249), Color.FromArgb(226, 229, 238), Color.White,
            Color.FromArgb(222, 226, 236), Color.FromArgb(205, 211, 226), Color.White, Color.FromArgb(22, 24, 34), Color.FromArgb(212, 22, 63)),
        "sand" => new Palette(Color.FromArgb(250, 245, 236), Color.FromArgb(236, 226, 208), Color.FromArgb(255, 252, 246),
            Color.FromArgb(234, 222, 200), Color.FromArgb(222, 207, 180), Color.FromArgb(255, 252, 246), Color.FromArgb(50, 38, 26), Color.FromArgb(200, 74, 40)),
        "sky" => new Palette(Color.FromArgb(238, 245, 252), Color.FromArgb(214, 228, 244), Color.White,
            Color.FromArgb(206, 222, 241), Color.FromArgb(184, 207, 234), Color.White, Color.FromArgb(18, 34, 56), Color.FromArgb(0, 102, 204)),
        _ => new Palette(Color.FromArgb(24, 24, 32), Color.FromArgb(18, 18, 24), Color.FromArgb(40, 40, 52),
            Color.FromArgb(44, 44, 58), Color.FromArgb(62, 62, 80), Color.FromArgb(40, 40, 52), Color.FromArgb(235, 235, 240), Color.FromArgb(250, 30, 78)),
    };

    private static string ThemeLabel(string mode) => mode switch
    {
        "system" => "System", "light" => "Light", "sand" => "Sand", "sky" => "Sky", _ => "Dark",
    };

    private static bool SystemIsLight()
    {
        try
        {
            using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return k?.GetValue("AppsUseLightTheme") is int v && v == 1;
        }
        catch { return false; }
    }

    private void ApplyChrome()
    {
        var p = CurrentPalette();

        BackColor = p.Bg;
        tabStrip.BackColor = p.Panel;
        tabFlow.BackColor = p.Panel;
        toolbar.BackColor = p.Bg;
        foreach (Control c in toolbar.Controls) c.BackColor = p.Bg;
        omnibox.BackColor = p.Field;
        omnibox.ForeColor = p.Fg;
        foreach (var b in new[] { back, fwd, reload, home, provider, askCopilot, askGemini, theme, newTabButton })
        {
            b.BackColor = p.Button;
            b.ForeColor = p.Fg;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = p.ButtonHover;
        }
        askCopilot.BackColor = Color.FromArgb(0, 120, 212);
        askCopilot.ForeColor = Color.White;
        askCopilot.FlatAppearance.MouseOverBackColor = Color.FromArgb(24, 140, 232);
        askGemini.BackColor = Color.FromArgb(66, 103, 214);
        askGemini.ForeColor = Color.White;
        askGemini.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 124, 232);
        provider.Text = "Search: " + Providers[providerIndex].Name;
        theme.Text = "Theme: " + ThemeLabel(themeMode);
        StyleShodanPane(p);
        foreach (var t in tabs) StyleHeader(t, p);

        var scheme = !IsLight ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
        foreach (var t in tabs)
        {
            t.View.DefaultBackgroundColor = p.Bg;
            if (t.View.CoreWebView2 != null) t.View.CoreWebView2.Profile.PreferredColorScheme = scheme;
        }
        PushSettingsToStartPage();
    }

    // Only the local start page receives settings; queries never enter the host bridge.
    private async void PushSettingsToStartPage()
    {
        foreach (var t in tabs.ToArray())
        {
            if (t.View.IsDisposed) continue;
            var core = t.View.CoreWebView2;
            if (core == null || !IsStartPage(core.Source)) continue;
            var themeValue = JsonSerializer.Serialize(themeMode);
            var providerValue = JsonSerializer.Serialize(Providers[providerIndex].Name.ToLowerInvariant());
            try
            {
                await core.ExecuteScriptAsync(
                    "if(location.origin==='https://kmr.start'&&location.pathname==='/index.html'){" +
                    $"window.kmrSetTheme&&kmrSetTheme({themeValue});window.kmrSetProvider&&kmrSetProvider({providerValue});}}");
            }
            catch (Exception ex) when (ex is InvalidOperationException or COMException)
            {
                System.Diagnostics.Trace.TraceError("Start-page settings sync failed: {0}", ex);
                if (!IsDisposed && !Disposing)
                    MessageBox.Show(this, "Could not synchronize start-page settings.\n\n" + ex.Message,
                        "K-Meleon-R", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void SetProvider(int index)
    {
        providerIndex = index;
        Settings.Set("provider", Providers[index].Name);
        ApplyChrome();
    }

    private void StyleHeader(BrowserTab t, Palette p)
    {
        t.Header.FlatAppearance.BorderSize = 0;
        t.Header.FlatAppearance.MouseOverBackColor = p.ButtonHover;
        t.Header.ForeColor = p.Fg;
        t.Header.BackColor = t == current ? p.TabActive : p.Panel;
    }

    private void CycleTheme(string requested = null)
    {
        themeMode = requested != null && Array.IndexOf(ThemeModes, requested) >= 0
            ? requested
            : ThemeModes[(Math.Max(0, Array.IndexOf(ThemeModes, themeMode)) + 1) % ThemeModes.Length];
        Settings.Set("theme", themeMode);
        ApplyChrome();
    }

    private async Task<BrowserTab> AddTab(string url, bool inPrivate = false)
    {
        var view = new WebView2 { Dock = DockStyle.Fill, Visible = false };
        var tab = new BrowserTab { View = view, IsPrivate = inPrivate };
        tab.Header = new Button
        {
            Width = 190, Height = 30, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(2, 4, 0, 0), TabStop = false, Text = inPrivate ? "[P] New Tab" : "New Tab",
        };
        tab.Header.Click += (_, e) =>
        {
            if (e is MouseEventArgs m && m.Button == MouseButtons.Middle) CloseTab(tab); else Select(tab);
        };
        tab.Header.MouseUp += (_, m) => { if (m.Button == MouseButtons.Middle) CloseTab(tab); };
        tabs.Add(tab);
        tabFlow.Controls.Add(tab.Header);
        host.Controls.Add(view);
        Select(tab);

        await view.EnsureCoreWebView2Async(env, inPrivate ? PrivateOptions() : null);
        var core = view.CoreWebView2;
        await HardenCore(core, inPrivate);
        core.ContainsFullScreenElementChanged += (_, _) => SetFullScreen(core.ContainsFullScreenElement);
        core.SourceChanged += (_, _) => { if (tab == current) ShowUrl(core.Source); };
        core.DocumentTitleChanged += (_, _) => { tab.Title = Shorten(core.DocumentTitle); tab.Header.Text = (tab.IsPrivate ? "[P] " : "") + tab.Title + "  "; if (tab == current) Text = WindowTitle(tab.Title == "New Tab" ? core.DocumentTitle : core.DocumentTitle); };
        core.NavigationCompleted += (_, _) => { UpdateNav(); PushSettingsToStartPage(); };
        core.WebMessageReceived += (_, e) =>
        {
            if (!IsStartPage(core.Source) || !IsStartPage(e.Source)) return;
            string msg;
            try
            {
                msg = e.TryGetWebMessageAsString();
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Trace.TraceWarning("Invalid start-page message: {0}", ex.Message);
                return;
            }
            if (msg != null && msg.StartsWith("theme:") && Array.IndexOf(ThemeModes, msg[6..]) >= 0)
                CycleTheme(msg[6..]);
            else if (msg != null && msg.StartsWith("provider:"))
            {
                var index = Array.FindIndex(Providers,
                    p => p.Name.Equals(msg[9..], StringComparison.OrdinalIgnoreCase));
                if (index >= 0) SetProvider(index);
                else System.Diagnostics.Trace.TraceWarning("Unknown start-page search provider.");
            }
            else System.Diagnostics.Trace.TraceWarning("Unrecognized start-page settings message.");
        };
        core.WindowCloseRequested += (_, _) => CloseTab(tab);
        view.KeyDown += (_, e) => HandleShortcut(e);
        ApplyChrome();
        core.Navigate(url);
        return tab;
    }

    private FormBorderStyle savedBorder; private FormWindowState savedState; private bool fullScreen;

    private void SetFullScreen(bool on)
    {
        if (on == fullScreen) return;
        fullScreen = on;
        if (on)
        {
            savedBorder = FormBorderStyle; savedState = WindowState;
            tabStrip.Visible = toolbar.Visible = false;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal; WindowState = FormWindowState.Maximized;
        }
        else
        {
            FormBorderStyle = savedBorder; WindowState = savedState;
            tabStrip.Visible = toolbar.Visible = true;
        }
    }

    private static string WindowTitle(string t) =>
        string.IsNullOrEmpty(t) || t == "K-Meleon-R" ? "K-Meleon-R" : t + " - K-Meleon-R";

    private static string Shorten(string s) => string.IsNullOrEmpty(s) ? "New Tab" : s.Length > 22 ? s[..22] + "\u2026" : s;

    private void ShowUrl(string src, bool force = false)
    {
        if (!force && omnibox.Modified) return;
        omnibox.Text = IsStartPage(src) ? "" : src ?? "";
        omnibox.Modified = false;
    }

    private void UpdateNav()
    {
        var core = current?.View.CoreWebView2;
        back.Enabled = core?.CanGoBack ?? false;
        fwd.Enabled = core?.CanGoForward ?? false;
    }

    private void Select(BrowserTab tab)
    {
        current = tab;
        foreach (var t in tabs) t.View.Visible = t == tab;
        tab.View.BringToFront();
        ApplyChrome();
        ShowUrl(tab.View.CoreWebView2?.Source, force: true);
        Text = WindowTitle(tab.View.CoreWebView2?.DocumentTitle);
        UpdateNav();
        if (omnibox.Text.Length == 0) omnibox.Focus(); else tab.View.Focus();
    }

    private void CloseTab(BrowserTab tab)
    {
        int i = tabs.IndexOf(tab);
        if (i < 0) return;
        tabs.RemoveAt(i);
        tabFlow.Controls.Remove(tab.Header);
        host.Controls.Remove(tab.View);
        tab.View.Dispose();
        if (tabs.Count == 0) { Close(); return; }
        if (tab == current) Select(tabs[Math.Min(i, tabs.Count - 1)]);
    }

    private void HandleShortcut(KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.T) { e.Handled = true; _ = AddTab(StartUrl); }
        else if (e.Control && e.Shift && e.KeyCode == Keys.N) { e.Handled = true; _ = AddTab(StartUrl, inPrivate: true); }
        else if (e.Control && e.Shift && e.KeyCode == Keys.S) { e.Handled = true; _ = ToggleShodan(); }
        else if (e.Control && e.KeyCode == Keys.W) { e.Handled = true; if (current != null) CloseTab(current); }
        else if (e.Control && e.KeyCode == Keys.L || e.KeyCode == Keys.F6) { e.Handled = true; omnibox.Focus(); }
        else if (e.Control && e.KeyCode == Keys.Tab && tabs.Count > 1)
        {
            e.Handled = true;
            int d = e.Shift ? -1 : 1;
            Select(tabs[(tabs.IndexOf(current) + d + tabs.Count) % tabs.Count]);
        }
        else if (e.KeyCode == Keys.F5) { current?.View.CoreWebView2?.Reload(); }
        else if (e.Alt && e.KeyCode == Keys.Home) { current?.View.CoreWebView2?.Navigate(StartUrl); }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        HandleShortcut(e);
        base.OnKeyDown(e);
    }

    private void Navigate(string text)
    {
        var url = Resolve(text, Providers[providerIndex]);
        if (url == null) return;
        if (current?.View.CoreWebView2 == null)
        {
            MessageBox.Show(this, "The browser is still starting. Please submit your search again in a moment.",
                "K-Meleon-R", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        omnibox.Modified = false;
        current.View.CoreWebView2.Navigate(url);
    }

    private CoreWebView2ControllerOptions PrivateOptions()
    {
        var opts = env.CreateCoreWebView2ControllerOptions();
        opts.IsInPrivateModeEnabled = true;   // in-memory profile: no history, cookies or cache on disk
        return opts;
    }

    // ── SHODAN side pane ──────────────────────────────────────────────────────────────────────
    private const string ShodanUrl = "https://kmr.shodan/shodan.html";

    private void BuildShodanPane()
    {
        var btns = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        var headerButtons = new[] { shodanMinBtn, shodanExpandBtn, shodanFullBtn, shodanFloatBtn, shodanCloseBtn };
        foreach (var b in headerButtons)
        {
            b.Margin = new Padding(1, 2, 1, 2);
            b.MinimumSize = new Size(26, 24);
        }
        btns.Controls.AddRange(headerButtons);
        shodanHeader.Controls.Add(shodanTitle);
        shodanHeader.Controls.Add(btns);
        shodanInner.Controls.Add(shodanBody);
        shodanInner.Controls.Add(shodanHeader);
        shodanDock.Controls.Add(shodanInner);
        shodanDock.Controls.Add(shodanRestoreRail);   // shown only while minimized to a rail

        var tips = new ToolTip();
        tips.SetToolTip(shodanMinBtn, "Minimize the pane to a rail / restore it");
        tips.SetToolTip(shodanExpandBtn, "Expand / collapse the pane width");
        tips.SetToolTip(shodanFullBtn, "Fullscreen the pane / exit fullscreen");
        tips.SetToolTip(shodanFloatBtn, "Pop the pane out into a floating window / dock it back");
        tips.SetToolTip(shodanCloseBtn, "Close the SHODAN pane");
    }

    private async Task ToggleShodan()
    {
        if (shodanFloat != null) { try { shodanFloat.Activate(); } catch { } return; }
        if (shodanDock.Visible)
        {
            if (shodanMinimized) { SetShodanMinimized(false); return; }
            CloseShodan();
            return;
        }
        shodanDock.Visible = true;
        shodanSplit.Visible = true;
        await EnsureShodanView();
    }

    private async Task EnsureShodanView()
    {
        if (shodanReady) return;
        shodanReady = true;
        shodanView = new WebView2 { Dock = DockStyle.Fill };
        shodanBody.Controls.Add(shodanView);
        try
        {
            await shodanView.EnsureCoreWebView2Async(env);
            var core = shodanView.CoreWebView2;
            await HardenCore(core, isPrivate: false);
            core.DocumentTitleChanged += (_, _) =>
            {
                var t = core.DocumentTitle;
                shodanTitle.Text = string.IsNullOrWhiteSpace(t) ? "SHODAN" : "SHODAN \u2014 " + t;
                if (shodanFloat != null) shodanFloat.Text = shodanTitle.Text;
            };
            if (Directory.Exists(Path.Combine(AppContext.BaseDirectory, "shodan")))
                core.Navigate(ShodanUrl);
            else
                ShowShodanMissing(core);
        }
        catch (Exception ex)
        {
            shodanReady = false;
            MessageBox.Show(this, "The SHODAN pane could not start.\n\n" + ex.Message,
                "K-Meleon-R", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void ShowShodanMissing(CoreWebView2 core)
    {
        DefenderGuard.AllowNextDataNavigation(core);
        core.NavigateToString(
            "<!doctype html><html><head><meta charset=\"utf-8\"><style>" +
            "body{background:#07100b;color:#9effc6;font:14px/1.6 'Segoe UI',sans-serif;display:flex;" +
            "align-items:center;justify-content:center;min-height:95vh;margin:0;text-align:center}" +
            "main{max-width:420px;padding:28px}h1{color:#3cff9e;font-size:18px}code{color:#d7ffe8}</style></head>" +
            "<body><main><h1>SHODAN is not bundled in this build</h1>" +
            "<p>The <code>shodan</code> folder was not shipped next to K-Meleon-R.exe. " +
            "Release builds bundle it automatically.</p></main></body></html>");
    }

    // Moves the live pane (header + SHODAN WebView2) between the dock and a float window WITHOUT
    // disposing it. All reparenting goes through here so the WebView2 is never destroyed by accident.
    private void AttachInner(Control parent)
    {
        if (shodanInner.IsDisposed || parent == null) return;
        if (shodanInner.Parent == parent) return;
        shodanInner.Parent?.Controls.Remove(shodanInner);
        parent.Controls.Add(shodanInner);
        shodanInner.Dock = DockStyle.Fill;
        shodanInner.BringToFront();
    }

    private void SetShodanExpanded(bool on)
    {
        shodanExpanded = on;
        if (shodanMinimized) SetShodanMinimized(false);
        if (shodanFloat != null)
        {
            if (!shodanFullscreen) shodanFloat.Width = (on ? ShodanExpandedWidth : ShodanNormalWidth) + 16;
        }
        else
        {
            shodanDock.Width = on ? ShodanExpandedWidth : ShodanNormalWidth;
        }
        shodanExpandBtn.Text = on ? "\u21A4\u21A6" : "\u2194";
    }

    private void ToggleShodanMinimized() => SetShodanMinimized(!shodanMinimized);

    // Minimize: when floating, minimize the OS window; when docked, collapse to a slim labelled rail.
    private void SetShodanMinimized(bool on)
    {
        if (shodanFloat != null)
        {
            try { shodanFloat.WindowState = on ? FormWindowState.Minimized : FormWindowState.Normal; } catch { }
            shodanMinimized = on;
            return;
        }
        shodanMinimized = on;
        shodanInner.Visible = !on;
        shodanRestoreRail.Visible = on;
        if (on) shodanRestoreRail.BringToFront(); else shodanInner.BringToFront();
        shodanDock.Width = on ? ShodanRailWidth : (shodanExpanded ? ShodanExpandedWidth : ShodanNormalWidth);
        shodanSplit.Visible = !on;
    }

    private void ToggleShodanFullscreen()
    {
        if (shodanFloat == null) FloatShodan();
        if (shodanFloat == null) return;
        SetShodanFullscreen(!shodanFullscreen);
    }

    private void SetShodanFullscreen(bool on)
    {
        var f = shodanFloat;
        if (f == null) return;
        if (on)
        {
            shodanFloatState = f.WindowState == FormWindowState.Minimized ? FormWindowState.Normal : f.WindowState;
            shodanFloatBorder = f.FormBorderStyle;
            f.WindowState = FormWindowState.Normal;   // required before changing the border style
            f.FormBorderStyle = FormBorderStyle.None;
            f.WindowState = FormWindowState.Maximized;
        }
        else
        {
            f.FormBorderStyle = shodanFloatBorder;
            f.WindowState = shodanFloatState;
        }
        shodanFullscreen = on;
        shodanFullBtn.Text = on ? "\u2715\u26F6" : "\u26F6";
    }

    private void FloatShodan()
    {
        if (shodanFloat != null) { try { shodanFloat.Activate(); } catch { } return; }
        if (shodanMinimized) SetShodanMinimized(false);
        var f = new Form
        {
            Text = shodanTitle.Text,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(shodanExpanded ? ShodanExpandedWidth : ShodanNormalWidth, 760),
            MinimumSize = new Size(300, 240),
            KeyPreview = true,
            ShowInTaskbar = true,
            BackColor = CurrentPalette().Panel,
        };
        try { f.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        shodanFloat = f;                 // set before reparenting so handlers see the floating state
        AttachInner(f);
        shodanDock.Visible = false;
        shodanSplit.Visible = false;
        shodanFloatBtn.Text = "\u2199";
        f.HandleCreated += (_, _) => { try { CaptureGuard.Apply(f, ShieldOn); } catch { } };
        f.FormClosing += (_, _) =>
        {
            // User closed the float window (OS button / Alt+F4): dock the live pane back out BEFORE
            // the form disposes its children, so the SHODAN session and WebView2 are never destroyed.
            // During the programmatic DockShodan() close, shodanFloat is already null, so this is inert.
            if (shodanFloat == f) DockShodanCore();
        };
        f.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape && shodanFullscreen) SetShodanFullscreen(false); };
        try { f.Show(this); } catch { }
    }

    // Reparents the live pane from the float window back under the dock and restores the docked UI
    // state. Never disposes anything; the caller decides whether to also close the empty float form.
    private void DockShodanCore()
    {
        shodanFloat = null;
        if (shodanFullscreen) { shodanFullscreen = false; shodanFullBtn.Text = "\u26F6"; }
        AttachInner(shodanDock);
        shodanFloatBtn.Text = "\u2197";
        shodanDock.Visible = true;
        shodanSplit.Visible = true;
        if (shodanMinimized) SetShodanMinimized(true);
    }

    private void DockShodan()
    {
        var f = shodanFloat;
        if (f == null) return;
        if (shodanFullscreen) SetShodanFullscreen(false);
        DockShodanCore();                // reparents the live pane out BEFORE the form is disposed
        try { if (!f.IsDisposed) f.Close(); } catch { }
        try { if (!f.IsDisposed) f.Dispose(); } catch { }
    }

    private void CloseShodan()
    {
        if (shodanFloat != null) DockShodan();   // reparents the live pane back safely first
        if (shodanMinimized) SetShodanMinimized(false);
        shodanDock.Visible = false;
        shodanSplit.Visible = false;
    }

    private void StyleShodanPane(Palette p)
    {
        shodanDock.BackColor = p.Panel;
        shodanInner.BackColor = p.Panel;
        shodanHeader.BackColor = p.Button;
        shodanBody.BackColor = p.Bg;
        shodanSplit.BackColor = p.Accent;
        shodanTitle.ForeColor = p.Fg;
        shodanRestoreRail.BackColor = p.Button;
        shodanRestoreRail.ForeColor = p.Fg;
        shodanRestoreRail.FlatAppearance.BorderSize = 0;
        shodanRestoreRail.FlatAppearance.MouseOverBackColor = p.ButtonHover;
        shodan.BackColor = p.Button;
        shodan.ForeColor = p.Fg;
        shodan.FlatAppearance.BorderSize = 0;
        shodan.FlatAppearance.MouseOverBackColor = p.ButtonHover;
        foreach (var b in new[] { shodanMinBtn, shodanExpandBtn, shodanFullBtn, shodanFloatBtn, shodanCloseBtn })
        {
            b.BackColor = p.Button;
            b.ForeColor = p.Fg;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = p.ButtonHover;
        }
        if (shodanFloat != null) shodanFloat.BackColor = p.Panel;
    }

    // Shared hardening applied to every WebView2 the shell creates: tabs, pop-up windows and the
    // SHODAN side pane. Keeps the guard stack identical everywhere so a pop-up is never a weak spot.
    private async Task HardenCore(CoreWebView2 core, bool isPrivate)
    {
        await CursorGuard.AttachAsync(core);
        CursorGuard.HardenBrowser(core);
        DefenderGuard.Attach(core);
        SafeBrowsingGuard.Attach(core);
        CaptureGuard.HardenBrowser(core);
        MapLocalHosts(core);
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.IsStatusBarEnabled = true;
        // Web pages (e.g. the AI WARP web edition) treat window.chrome.webview as a native app host; only the start page may see it.
        await core.AddScriptToExecuteOnDocumentCreatedAsync("if(location.hostname!=='kmr.start'&&window.chrome){try{Object.defineProperty(window.chrome,'webview',{value:undefined,configurable:true})}catch(e){}}");
        core.Settings.AreDevToolsEnabled = true;
        core.Settings.AreBrowserAcceleratorKeysEnabled = true;
        core.Settings.IsGeneralAutofillEnabled = true;
        core.Settings.IsPasswordAutosaveEnabled = true;
        core.Settings.IsZoomControlEnabled = true;
        core.Settings.IsSwipeNavigationEnabled = true;
        // SmartScreen reputation checking is forced on by DefenderGuard.Attach above.
        // Balanced keeps embedded players (YouTube, Spotify) working while still blocking known trackers.
        core.Profile.PreferredTrackingPreventionLevel = Settings.Get("tracking", "balanced") switch
        {
            "none" => CoreWebView2TrackingPreventionLevel.None,
            "strict" => CoreWebView2TrackingPreventionLevel.Strict,
            _ => CoreWebView2TrackingPreventionLevel.Balanced,
        };
        core.ProcessFailed += (_, e) =>
        {
            if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessExited ||
                e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
                BeginInvoke(new Action(() => { try { core.Reload(); } catch { } }));
        };
        core.PermissionRequested += (_, e) =>
        {
            if (e.PermissionKind == CoreWebView2PermissionKind.ClipboardRead)
                e.State = CoreWebView2PermissionState.Allow;
        };
        core.NewWindowRequested += async (_, e) => await HandleNewWindowRequested(e, isPrivate);
    }

    private static void MapLocalHosts(CoreWebView2 core)
    {
        core.SetVirtualHostNameToFolderMapping("kmr.start", Path.Combine(AppContext.BaseDirectory, "start"),
            CoreWebView2HostResourceAccessKind.Allow);
        var shodanDir = Path.Combine(AppContext.BaseDirectory, "shodan");
        if (Directory.Exists(shodanDir))
            core.SetVirtualHostNameToFolderMapping("kmr.shodan", shodanDir,
                CoreWebView2HostResourceAccessKind.Allow);
    }

    // Hardened pop-up handler. Pop-ups are allowed to load their content, but never silently:
    //  * a verified-malicious target (Google Safe Browsing) is refused outright;
    //  * anything else opens in its own guarded window carrying an "allowed pop-up" notice so a
    //    page can never sneak a background window past the user. Content still loads either way.
    private async Task HandleNewWindowRequested(CoreWebView2NewWindowRequestedEventArgs e, bool isPrivate)
    {
        var deferral = e.GetDeferral();
        try
        {
            var uri = e.Uri;
            string threat = null;
            try { threat = await SafeBrowsingGuard.CheckAsync(uri); }
            catch { threat = null; }   // a failed check never blocks a pop-up
            if (threat != null)
            {
                e.Handled = true;
                PopupWindow.ShowBlocked(this, uri, threat);
                return;
            }

            var popup = new PopupWindow(this, isPrivate, ShieldOn);
            var core = await popup.CreateCoreAsync(env, isPrivate ? PrivateOptions() : null,
                c => HardenCore(c, isPrivate));
            if (core == null) { e.Handled = true; return; }
            e.NewWindow = core;   // WebView2 drives the pop-up into our guarded window
            ApplyPopupWindowFeatures(popup, e.WindowFeatures);
            popup.Show(this);
        }
        finally { deferral.Complete(); }
    }

    private static void ApplyPopupWindowFeatures(PopupWindow popup, CoreWebView2WindowFeatures f)
    {
        try
        {
            var w = f != null && f.HasSize && f.Width >= 240 ? (int)f.Width : 900;
            var h = f != null && f.HasSize && f.Height >= 180 ? (int)f.Height : 680;
            popup.ClientSize = new Size(Math.Min(w, 1600), Math.Min(h + PopupWindow.BannerHeight, 1200));
        }
        catch { }
    }

    // Opens a URL as a brand-new guarded tab (used by pop-up windows that the user promotes).
    internal Task PromoteToTab(string url, bool isPrivate) => AddTab(url, isPrivate);

    private async Task Ask(string providerName)
    {
        var url = Providers.Single(p => p.Name == providerName).Search(omnibox.Text);
        await AddTab(url, current?.IsPrivate ?? false);
    }
}
