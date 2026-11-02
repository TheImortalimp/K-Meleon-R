# K-Meleon-R r14 (8.6.0) - Defender/Safe Browsing watchdogs, anti-screencap, private tabs

Final hardening pass: the Microsoft and Google protection hooks now run under
the hood on every tab, the window can hide itself from screen capture, and
Copilot/Gemini get first-class query support.

## Microsoft Defender integration (DefenderGuard)

- **SmartScreen on:** WebView2 reputation checking is now forced on for every
  navigation and download (it was previously disabled).
- **Silent self-healing:** the Defender hooks are re-asserted on launch and
  every minute while the browser runs, on every live tab, with no UI - if
  anything switches SmartScreen off it is silently switched back on, and the
  capture shield is re-applied in the same cycle. Guard logs are size-capped.
- **Download scanning:** every completed download is handed to the Windows
  Attachment Execution Service (`IAttachmentExecute.Save`), which stamps the
  Mark-of-the-Web and invokes the registered antivirus - Microsoft Defender by
  default. A second pass scans the file bytes through AMSI, the same interface
  Defender exposes for script scanning. Flagged files are removed and the user
  is told why. If the attachment service is unavailable, the Mark-of-the-Web is
  written directly so Defender still screens the file on open.
- **Security Center watchdog:** Windows Security Center (`root\SecurityCenter2`)
  is polled every five minutes; the user is warned when no real-time antivirus
  protection is active or definitions are stale.
- **Phishing filter:** `javascript:`, `data:` and `vbscript:` top-level
  navigations are refused; punycode (`xn--`) hosts are logged as possible IDN
  homograph phishing. Log: `%LOCALAPPDATA%\K-Meleon-R\defender-guard.log`.

## Google Safe Browsing integration (SafeBrowsingGuard)

- Navigations are checked against the Google Safe Browsing v4 blocklists
  (malware, social engineering, unwanted software, harmful applications) with
  verdict caching; flagged pages are stopped and replaced by a local warning
  page.
- Requires a (free) Safe Browsing API key: set `gsbApiKey` in
  `%LOCALAPPDATA%\K-Meleon-R\settings.json` or the `KMR_GSB_API_KEY`
  environment variable. Without a key the guard is silent and SmartScreen still
  provides reputation checking. Lookups send only the visited URL to Google.

## Anti-screencap hardening (CaptureGuard)

- **Capture shield (on by default, no UI):** the whole browser window is
  excluded from screenshots, screen recording and remote-viewing tools via
  `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` - capture tools see black
  where the browser is. The shield runs invisibly and is silently re-applied
  each guard cycle; it can only be disabled by setting `"shield": "off"` in
  `settings.json` (it affects the user's own screenshots too).
- **Inbound block:** web pages can never start a screen capture
  (`getDisplayMedia`); the WebView2 `ScreenCaptureStarting` request is always
  cancelled, so a phishing page cannot trick the user into streaming their
  desktop.

## Privacy

- **Private tabs:** Ctrl+Shift+N opens an InPrivate tab (marked `[P]`) using an
  in-memory profile - no history, cookies or cache on disk. Links opened from a
  private tab stay private.
- **Tracking prevention** is raised from Off to **Balanced** by default
  (settings key `tracking`: `none` / `balanced` / `strict`); Balanced keeps
  embedded players (YouTube, Spotify) working.

## Copilot and Gemini

- **Ask Copilot** now opens `copilot.microsoft.com` with the query passed
  directly in the URL.
- **Ask Gemini** now opens Google AI Mode (the Gemini-powered `udm=50` search)
  with the query, replacing the old copy-to-clipboard workaround; without a
  query it opens the Gemini app.
- New **Gemini** entry in the search-provider cycle (Bing / Google / Copilot /
  Gemini).

## Installer and build

- Installer version r14; the hardening scripts now run with
  `-ExecutionPolicy RemoteSigned` instead of `Bypass`.
- **License and security notices page:** the installer shows a notices page
  with a .NET Runtime MIT license tribute (the app ships the self-contained
  .NET 8 runtime), the WebView2 license note and an explicit
  SmartScreen/administrator warning dialog before installing.
- **Non-admin installs:** elevation is no longer required
  (`RequestExecutionLevel highest`). Without administrator rights the browser
  installs per-user and the Windows policy hardening steps are skipped; the
  scripts are installed alongside the app for later elevated use.
- **Publisher metadata:** installer version info and the Windows uninstall
  entry carry imortalimp.nl (publisher, about link).
- WebView2 SDK updated to 1.0.4314.35 (needed for the screen-capture block);
  `System.Management` added for the Security Center watchdog; the unused
  WebView2 WPF assembly is pruned from the build.

## Limits

- The capture shield needs Windows 10 2004+ and hides the window from the
  user's own screenshots too (set `"shield": "off"` in settings.json to
  capture; there is intentionally no button for it).
- Download scanning and the watchdog depend on the installed antivirus and on
  Security Center (absent on Windows Server); Safe Browsing needs an API key.
- An administrator can still undo policies; the installer remains unsigned, so
  SmartScreen warns (code signing is the real fix).
- A clean scan is not proof of safety; these are layered defenses, not a
  guarantee.

## Version and download

- App version: **8.6.0**; release label: **r14**; tag: **v8.6.0-r14**.
- [Download K-Meleon-R-r14-x64-setup.exe](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.6.0-r14/K-Meleon-R-r14-x64-setup.exe)
  (installer, applies the Windows hardening policies under UAC) or
  [K-Meleon-R-r14-x64-portable.zip](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.6.0-r14/K-Meleon-R-r14-x64-portable.zip)
  (portable; the hardening scripts are included but must be run manually).
- Requires Windows 10/11 x64 and Microsoft Edge WebView2.

SHA-256:

```text
K-Meleon-R-r14-x64-setup.exe    440808FF57BA7DC881CE41719A8E8C2ED3E2B0A48019EE6431A0C18B94D9556E
K-Meleon-R-r14-x64-portable.zip A876BB2810E0A945546D9D3AF940BB87531C210D47B46109D426BC2B8B0F91EE
```

## Retained changes

- **r13 (8.5.0):** theme/pointer package download/navigation blocking,
  `ms-settings:` blocking, sound-scheme policy, elevated GEM OS hardening.
- **r12 (8.4.0):** system-pointer-only page/runtime guards, pointer-path
  validation, process hardening and elevated cursor lockdown with backups.
- **r11 (8.3.0):** native host bridge hidden from ordinary web pages.
- **r10/r9:** WebView2 playback/fullscreen/recovery handling, editable speed
  dials and five synchronized themes.
