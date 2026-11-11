using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace KMeleonR;

// A guarded pop-up window. The hardened pop-up handler only ever opens content that is not
// verified malicious, and always in one of these: a visible, titled window carrying an
// "allowed pop-up" notice and a one-click "Open as tab" promotion. The content loads normally;
// the window just makes sure a page can never open a hidden background pop-up, and it inherits
// the same guard stack (Cursor/Defender/SafeBrowsing/Capture) as every tab.
internal sealed class PopupWindow : Form
{
    public const int BannerHeight = 30;

    private readonly MainForm owner;
    private readonly bool isPrivate;
    private readonly bool shieldOn;
    private readonly Label notice = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(10, 0, 0, 0),
        AutoEllipsis = true,
        Font = new Font("Segoe UI", 9f),
        ForeColor = Color.FromArgb(235, 235, 240),
    };
    private readonly Button openTab = new()
    {
        Text = "Open as tab",
        Dock = DockStyle.Right,
        Width = 104,
        FlatStyle = FlatStyle.Flat,
        TabStop = false,
        ForeColor = Color.White,
        BackColor = Color.FromArgb(0, 120, 212),
    };
    private WebView2 view;
    private string currentUrl = "";

    public PopupWindow(MainForm owner, bool isPrivate, bool shieldOn)
    {
        this.owner = owner;
        this.isPrivate = isPrivate;
        this.shieldOn = shieldOn;

        Text = "K-Meleon-R pop-up";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 680);
        KeyPreview = true;
        BackColor = Color.FromArgb(24, 24, 32);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        var banner = new Panel { Dock = DockStyle.Top, Height = BannerHeight, BackColor = Color.FromArgb(44, 44, 58) };
        notice.Text = (isPrivate ? "[P] " : "") + "Pop-up allowed \u2014 loading\u2026";
        openTab.FlatAppearance.BorderSize = 0;
        openTab.FlatAppearance.MouseOverBackColor = Color.FromArgb(24, 140, 232);
        openTab.Click += (_, _) =>
        {
            if (string.IsNullOrEmpty(currentUrl)) return;
            _ = owner.PromoteToTab(currentUrl, isPrivate);
            Close();
        };
        banner.Controls.Add(notice);
        banner.Controls.Add(openTab);
        Controls.Add(banner);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CaptureGuard.Apply(this, shieldOn);   // the pop-up is screen-capture shielded too
    }

    // Creates the pop-up's WebView2 core, hardens it with the shared guard stack and returns it so
    // WebView2 can drive the opener's content into this window. Returns null if creation fails.
    public async Task<CoreWebView2> CreateCoreAsync(
        CoreWebView2Environment env, CoreWebView2ControllerOptions opts, Func<CoreWebView2, Task> harden)
    {
        view = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(view);
        view.BringToFront();
        try { await view.EnsureCoreWebView2Async(env, opts); }
        catch { return null; }
        var core = view.CoreWebView2;
        await harden(core);
        core.SourceChanged += (_, _) => UpdateNotice(core.Source);
        core.DocumentTitleChanged += (_, _) =>
            Text = string.IsNullOrEmpty(core.DocumentTitle) ? "K-Meleon-R pop-up" : core.DocumentTitle + " \u2014 pop-up";
        core.WindowCloseRequested += (_, _) => Close();
        view.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        return core;
    }

    private void UpdateNotice(string url)
    {
        currentUrl = url ?? "";
        string host;
        try { host = new Uri(currentUrl).Host; } catch { host = currentUrl; }
        notice.Text = (isPrivate ? "[P] " : "") + "Pop-up allowed"
            + (string.IsNullOrEmpty(host) ? "" : " \u2014 " + host);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) view?.Dispose();
        base.Dispose(disposing);
    }

    // Shown instead of a window when the pop-up target is verified malicious: nothing loads.
    public static void ShowBlocked(MainForm owner, string url, string threat)
    {
        string host;
        try { host = new Uri(url).Host; } catch { host = url; }
        MessageBox.Show(owner,
            "A pop-up to " + host + " was blocked before it could load.\n\n" +
            "Google Safe Browsing reports it as " + SafeBrowsingGuard.Describe(threat) + ".",
            "K-Meleon-R blocked a dangerous pop-up", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
