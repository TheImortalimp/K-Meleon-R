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
    private readonly Panel host = new() { Dock = DockStyle.Fill };
    private readonly List<BrowserTab> tabs = new();
    private BrowserTab current;
    private CoreWebView2Environment env;
    private readonly string initialUrl;
    private int providerIndex;
    private string themeMode;

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
        right.Controls.AddRange(new Control[] { provider, askCopilot, askGemini, theme });
        var left = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false, Padding = new Padding(4, 4, 0, 0) };
        left.Controls.AddRange(new Control[] { back, fwd, reload, home });
        omnibox.Dock = DockStyle.Fill;
        var omniHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 8, 6, 6) };
        omniHost.Controls.Add(omnibox);
        toolbar.Controls.Add(omniHost);
        toolbar.Controls.Add(right);
        toolbar.Controls.Add(left);

        Controls.Add(host);
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
        await CursorGuard.AttachAsync(core);
        CursorGuard.HardenBrowser(core);
        DefenderGuard.Attach(core);
        SafeBrowsingGuard.Attach(core);
        CaptureGuard.HardenBrowser(core);
        core.SetVirtualHostNameToFolderMapping("kmr.start", Path.Combine(AppContext.BaseDirectory, "start"),
            CoreWebView2HostResourceAccessKind.Allow);
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
        core.ContainsFullScreenElementChanged += (_, _) => SetFullScreen(core.ContainsFullScreenElement);
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
        core.NewWindowRequested += async (_, e) =>
        {
            e.Handled = true;
            await AddTab(e.Uri, tab.IsPrivate);
        };
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

    private async Task Ask(string providerName)
    {
        var url = Providers.Single(p => p.Name == providerName).Search(omnibox.Text);
        await AddTab(url, current?.IsPrivate ?? false);
    }
}
