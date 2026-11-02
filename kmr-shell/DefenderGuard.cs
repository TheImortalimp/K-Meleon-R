using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;

namespace KMeleonR;

// Microsoft Defender / Windows security integration. Four layers, all automatic:
//  1. SmartScreen: WebView2 reputation checking is forced on for every navigation and download.
//  2. Downloads: every completed download goes through the Windows Attachment Execution Service
//     (IAttachmentExecute.Save), which stamps the Mark-of-the-Web and hands the file to the
//     registered antivirus (Microsoft Defender by default). A second pass scans the bytes through
//     AMSI, the same interface Defender uses for script scanning. Flagged files are removed.
//  3. Watchdog: Windows Security Center is polled so the user is warned when no real-time
//     antivirus protection is active or definitions are stale.
//  4. Phishing filter: javascript:, data: and vbscript: top-level navigations are refused and
//     punycode (IDN homograph) hosts are logged.
internal static class DefenderGuard
{
    private static string logPath = "";
    private static Action<string> notify = _ => { };
    private static System.Windows.Forms.Timer watchdog;
    private static string lastAvWarning;
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CoreWebView2, object> expectedDataNav = new();

    // SafeBrowsingGuard renders its local warning via NavigateToString, which WebView2 implements
    // as a data: navigation; this one-shot token keeps the phishing filter from cancelling it.
    public static void AllowNextDataNavigation(CoreWebView2 core)
    {
        expectedDataNav.Remove(core);
        expectedDataNav.Add(core, new object());
    }

    public static void Init(string appName, Action<string> notifyUser)
    {
        logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName, "defender-guard.log");
        notify = notifyUser ?? notify;
        watchdog = new System.Windows.Forms.Timer { Interval = 300_000 };
        watchdog.Tick += (_, _) => CheckSecurityCenter();
        watchdog.Start();
        CheckSecurityCenter();
    }

    public static void Attach(CoreWebView2 core)
    {
        core.Settings.IsReputationCheckingRequired = true;   // SmartScreen URL + download reputation

        core.NavigationStarting += (_, e) =>
        {
            var uri = e.Uri ?? "";
            if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) &&
                expectedDataNav.TryGetValue(core, out _))
            {
                expectedDataNav.Remove(core);   // the Safe Browsing warning page
                return;
            }
            if (uri.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                Log("blocked script-scheme navigation: " + Shorten(uri));
                return;
            }
            try
            {
                var host = new Uri(uri).IdnHost;
                if (new Uri(uri).Host.Contains("xn--", StringComparison.OrdinalIgnoreCase))
                    Log($"punycode host (possible IDN homograph): {new Uri(uri).Host} -> {host}");
            }
            catch { }
        };

        core.DownloadStarting += (_, e) =>
        {
            var op = e.DownloadOperation;
            var source = op.Uri;
            op.StateChanged += (_, _) =>
            {
                if (op.State != CoreWebView2DownloadState.Completed) return;
                var path = op.ResultFilePath;
                _ = Task.Run(() =>
                {
                    var verdict = ScanDownload(path, source);
                    if (verdict != null)
                        notify($"Microsoft Defender flagged a download and it was removed.\n\nFile: {Path.GetFileName(path)}\nReason: {verdict}");
                });
            };
        };
    }

    // Returns null when the file is clean/kept, otherwise a short description of why it was removed.
    public static string ScanDownload(string path, string sourceUrl)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        // Layer 1: Attachment Execution Service - Mark-of-the-Web plus the registered AV engine.
        try
        {
            var type = Type.GetTypeFromCLSID(new Guid("4125DD96-E03A-4103-8F70-E0597D803B9C"));
            var ae = (IAttachmentExecute)Activator.CreateInstance(type);
            try
            {
                var guid = ClientGuid;
                ae.SetClientGuid(ref guid);
                ae.SetLocalPath(path);
                if (!string.IsNullOrEmpty(sourceUrl)) ae.SetSource(sourceUrl);
                var hr = ae.Save();
                if (hr != 0)
                {
                    Log($"attachment service rejected download (0x{hr:X8}): {path}");
                    TryDelete(path);
                    return "blocked by the Windows attachment security scan";
                }
                Log("defender scan ok: " + path);
            }
            finally { Marshal.ReleaseComObject(ae); }
        }
        catch (Exception ex)
        {
            Log("attachment service unavailable, writing Mark-of-the-Web directly: " + ex.Message);
            WriteMotw(path, sourceUrl);
        }

        // Layer 2: AMSI - the in-memory scan interface Defender exposes to applications.
        try
        {
            if (File.Exists(path) && AmsiFlags(path))
            {
                TryDelete(path);
                return "detected by the antimalware (AMSI) scan";
            }
        }
        catch (Exception ex) { Log("amsi scan skipped: " + ex.Message); }
        return File.Exists(path) ? null : "removed by the antivirus engine";
    }

    private static readonly Guid ClientGuid = new("8ba2c4a8-21bb-4a9a-98c9-7e2c1f3a0d14");

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { }
    }

    private static void WriteMotw(string path, string sourceUrl)
    {
        try
        {
            using var s = new StreamWriter(new FileStream(path + ":Zone.Identifier", FileMode.Create, FileAccess.Write));
            s.WriteLine("[ZoneTransfer]");
            s.WriteLine("ZoneId=3");
            if (!string.IsNullOrEmpty(sourceUrl)) s.WriteLine("HostUrl=" + sourceUrl);
        }
        catch (Exception ex) { Log("mark-of-the-web failed: " + ex.Message); }
    }

    private const int MaxAmsiBytes = 64 * 1024 * 1024;

    private static bool AmsiFlags(string path)
    {
        if (AmsiInitialize("K-Meleon-R", out var ctx) != 0) return false;
        try
        {
            using var fs = File.OpenRead(path);
            var buffer = new byte[Math.Min(fs.Length, MaxAmsiBytes)];
            var read = fs.Read(buffer, 0, buffer.Length);
            if (read <= 0) return false;
            if (AmsiScanBuffer(ctx, buffer, (uint)read, Path.GetFileName(path), 0, out var result) != 0) return false;
            if (result >= 32768) { Log($"AMSI detection ({result}): {path}"); return true; }   // AMSI_RESULT_DETECTED
            return false;
        }
        finally { AmsiUninitialize(ctx); }
    }

    private static void CheckSecurityCenter()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\SecurityCenter2", "SELECT displayName, productState FROM AntiVirusProduct");
            var enabled = new List<string>();
            var stale = new List<string>();
            var total = 0;
            foreach (var av in searcher.Get())
            {
                total++;
                var name = av["displayName"] as string ?? "antivirus";
                var state = Convert.ToUInt32(av["productState"]);
                if (((state >> 8) & 0xFF) is 0x10 or 0x11)   // real-time protection on
                {
                    enabled.Add(name);
                    if ((state & 0xFF) != 0) stale.Add(name);   // definitions out of date
                }
            }
            var warn =
                total == 0 ? "No antivirus product is registered with Windows Security Center." :
                enabled.Count == 0 ? "Real-time antivirus protection is OFF. Turn Microsoft Defender real-time protection back on before browsing." :
                stale.Count > 0 ? "Antivirus definitions are out of date for: " + string.Join(", ", stale) + "." :
                null;
            if (warn == null) { lastAvWarning = null; return; }
            if (warn == lastAvWarning) return;
            lastAvWarning = warn;
            Log("watchdog: " + warn);
            notify(warn);
        }
        catch (Exception ex) { Log("watchdog unavailable: " + ex.Message); }   // e.g. Windows Server has no SecurityCenter2
    }

    private static string Shorten(string s) => s.Length > 120 ? s[..120] + "\u2026" : s;

    private static void Log(string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"{DateTime.Now:s} {line}{Environment.NewLine}");
        }
        catch { }
    }

    [DllImport("amsi.dll", CharSet = CharSet.Unicode)]
    private static extern int AmsiInitialize(string appName, out nint context);

    [DllImport("amsi.dll")]
    private static extern void AmsiUninitialize(nint context);

    [DllImport("amsi.dll", CharSet = CharSet.Unicode)]
    private static extern int AmsiScanBuffer(nint context, byte[] buffer, uint length, string contentName, nint session, out int result);

    [ComImport, Guid("73DB1241-1E85-4581-8E4F-A81E1D0F8C57"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAttachmentExecute
    {
        void SetClientTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
        void SetClientGuid(ref Guid guid);
        void SetLocalPath([MarshalAs(UnmanagedType.LPWStr)] string localPath);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string fileName);
        void SetSource([MarshalAs(UnmanagedType.LPWStr)] string source);
        void SetReferrer([MarshalAs(UnmanagedType.LPWStr)] string referrer);
        [PreserveSig] int CheckPolicy();
        void Prompt(nint hwnd, int prompt, out int action);
        [PreserveSig] int Save();
        void Execute(nint hwnd, [MarshalAs(UnmanagedType.LPWStr)] string verb, out nint processHandle);
        void SaveWithUI(nint hwnd);
        void ClearClientState();
    }
}
