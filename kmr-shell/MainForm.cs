using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace KMeleonR;

internal sealed class BrowserTab
{
    public WebView2 View;
    public Button Header;
    public string Title = "New Tab";
}

internal sealed class MainForm : Form
{
    private const string StartUrl = "https://kmr.start/index.html";

    private static readonly (string Name, string Url)[] Providers =
    {
        ("Bing", "https://www.bing.com/search?q={0}"),
        ("Google", "https://www.google.com/search?q={0}"),
        ("Copilot", "https://www.bing.com/copilotsearch?q={0}"),
    };

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
            providerIndex = (providerIndex + 1) % Providers.Length;
            Settings.Set("provider", Providers[providerIndex].Name);
            ApplyChrome();
        };
        askCopilot.Click += async (_, _) => await AskCopilot();
        askGemini.Click += async (_, _) => await AskGemini();
        theme.Click += (_, _) =>
        {
            themeMode = themeMode switch { "dark" => "light", "light" => "system", _ => "dark" };
            Settings.Set("theme", themeMode);
            ApplyChrome();
        };

        ApplyChrome();
        Shown += async (_, _) => await Start();
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
            env = await CoreWebView2Environment.CreateAsync(null, Settings.ProfileDir);
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
        await AddTab(string.IsNullOrWhiteSpace(initialUrl) ? StartUrl : Normalize(initialUrl));
    }

    private bool IsLight => themeMode == "light" || (themeMode == "system" && SystemIsLight());

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
        bool light = IsLight;
        var bg = light ? Color.FromArgb(236, 238, 242) : Color.FromArgb(24, 24, 32);
        var panel = light ? Color.FromArgb(222, 225, 232) : Color.FromArgb(18, 18, 24);
        var fg = light ? Color.FromArgb(25, 25, 30) : Color.FromArgb(235, 235, 240);
        var accent = Color.FromArgb(250, 30, 78);

        BackColor = bg;
        tabStrip.BackColor = panel;
        tabFlow.BackColor = panel;
        toolbar.BackColor = bg;
        foreach (Control c in toolbar.Controls) c.BackColor = bg;
        omnibox.BackColor = light ? Color.White : Color.FromArgb(40, 40, 52);
        omnibox.ForeColor = fg;
        foreach (var b in new[] { back, fwd, reload, home, provider, askCopilot, askGemini, theme, newTabButton })
        {
            b.BackColor = light ? Color.FromArgb(208, 212, 222) : Color.FromArgb(44, 44, 58);
            b.ForeColor = fg;
            b.FlatAppearance.BorderSize = 0;
        }
        askCopilot.BackColor = Color.FromArgb(0, 120, 212);
        askCopilot.ForeColor = Color.White;
        askGemini.BackColor = Color.FromArgb(66, 103, 214);
        askGemini.ForeColor = Color.White;
        provider.Text = "Search: " + Providers[providerIndex].Name;
        theme.Text = "Theme: " + (themeMode == "system" ? "System" : themeMode == "light" ? "Light" : "Dark");
        foreach (var t in tabs) StyleHeader(t, panel, fg, accent);

        var scheme = themeMode == "light" ? CoreWebView2PreferredColorScheme.Light
            : themeMode == "system" ? CoreWebView2PreferredColorScheme.Auto : CoreWebView2PreferredColorScheme.Dark;
        foreach (var t in tabs)
            if (t.View.CoreWebView2 != null) t.View.CoreWebView2.Profile.PreferredColorScheme = scheme;
    }

    private void StyleHeader(BrowserTab t, Color panel, Color fg, Color accent)
    {
        t.Header.FlatAppearance.BorderSize = 0;
        t.Header.ForeColor = fg;
        t.Header.BackColor = t == current ? (IsLight ? Color.White : Color.FromArgb(40, 40, 52)) : panel;
    }

    private async Task<BrowserTab> AddTab(string url)
    {
        var view = new WebView2 { Dock = DockStyle.Fill, Visible = false };
        var tab = new BrowserTab { View = view };
        tab.Header = new Button
        {
            Width = 190, Height = 30, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(2, 4, 0, 0), TabStop = false, Text = "New Tab",
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

        await view.EnsureCoreWebView2Async(env);
        var core = view.CoreWebView2;
        core.SetVirtualHostNameToFolderMapping("kmr.start", Path.Combine(AppContext.BaseDirectory, "start"),
            CoreWebView2HostResourceAccessKind.Allow);
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.IsStatusBarEnabled = true;
        core.NewWindowRequested += async (_, e) =>
        {
            e.Handled = true;
            await AddTab(e.Uri);
        };
        core.SourceChanged += (_, _) => { if (tab == current) ShowUrl(core.Source); };
        core.DocumentTitleChanged += (_, _) => { tab.Title = Shorten(core.DocumentTitle); tab.Header.Text = tab.Title + "  "; if (tab == current) Text = WindowTitle(tab.Title == "New Tab" ? core.DocumentTitle : core.DocumentTitle); };
        core.NavigationCompleted += (_, _) => UpdateNav();
        core.WindowCloseRequested += (_, _) => CloseTab(tab);
        view.KeyDown += (_, e) => HandleShortcut(e);
        ApplyChrome();
        core.Navigate(url);
        return tab;
    }

    private static string WindowTitle(string t) =>
        string.IsNullOrEmpty(t) || t == "K-Meleon-R" ? "K-Meleon-R" : t + " - K-Meleon-R";

    private static string Shorten(string s) => string.IsNullOrEmpty(s) ? "New Tab" : s.Length > 22 ? s[..22] + "\u2026" : s;

    private void ShowUrl(string src)
    {
        omnibox.Text = src != null && src.StartsWith(StartUrl, StringComparison.OrdinalIgnoreCase) ? "" : src ?? "";
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
        ShowUrl(tab.View.CoreWebView2?.Source);
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

    private static string Normalize(string input)
    {
        input = input.Trim();
        if (input.Contains("://") || input.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return input;
        if (!input.Contains(' ') && (input.Contains('.') || input.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)))
            return "https://" + input;
        return null;
    }

    private void Navigate(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || current?.View.CoreWebView2 == null) return;
        var url = Normalize(text) ?? string.Format(Providers[providerIndex].Url, Uri.EscapeDataString(text.Trim()));
        current.View.CoreWebView2.Navigate(url);
    }

    private async Task AskCopilot()
    {
        var q = omnibox.Text.Trim();
        var url = q.Length > 0 && Normalize(q) == null
            ? string.Format(Providers[2].Url, Uri.EscapeDataString(q))
            : "https://www.bing.com/copilotsearch";
        await AddTab(url);
    }

    private async Task AskGemini()
    {
        var q = omnibox.Text.Trim();
        if (q.Length > 0 && Normalize(q) == null)
        {
            try { Clipboard.SetText(q); } catch { }
        }
        await AddTab("https://gemini.google.com/app");
    }
}
