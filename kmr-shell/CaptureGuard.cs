using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;

namespace KMeleonR;

// Anti-screencap hardening. Two directions:
//  1. Outbound: the whole browser window is excluded from screenshots, screen recording and
//     remote-viewing tools via SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE) - capture tools
//     see black where the browser is. Toggleable from the Shield button (the user's own
//     screenshots are affected too), on by default.
//  2. Inbound: web pages are never allowed to start a screen capture (getDisplayMedia), so a
//     phishing page cannot trick the user into streaming their desktop.
internal static class CaptureGuard
{
    private const uint WDA_NONE = 0x0;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;   // Windows 10 2004+

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(nint hWnd, uint dwAffinity);

    public static bool Apply(Form form, bool shieldOn)
    {
        try
        {
            return form.IsHandleCreated &&
                   SetWindowDisplayAffinity(form.Handle, shieldOn ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE);
        }
        catch { return false; }
    }

    public static void HardenBrowser(CoreWebView2 core)
    {
        try
        {
            core.ScreenCaptureStarting += (_, e) => { e.Cancel = true; e.Handled = true; };
        }
        catch { }   // older WebView2 runtimes without the event keep the remaining layers
    }
}
