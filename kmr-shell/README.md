# K-Meleon-R shell r15 (8.7.0)

**Current release as of 2 November 2026: r15 (8.7.0), the query-search fix
and general bug-fix build.** It supersedes r14 and earlier versions and is
available as a Windows x64 EXE installer and a portable ZIP.

The current Windows browser is a .NET 8 WinForms application using Microsoft
Edge WebView2. It reuses the K-Meleon-R start-page design, not the legacy
Gecko/Goanna C++ implementation. Pale Moon is not required.

[Project README](https://github.com/TheImortalimp/K-Meleon-R/blob/v8.7.0-r15/README.md) |
[About](https://github.com/TheImortalimp/K-Meleon-R/blob/v8.7.0-r15/ABOUT.md) |
[r15 release notes and checksums](https://github.com/TheImortalimp/K-Meleon-R/releases/tag/v8.7.0-r15) |
[Latest release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest) |
[Included release notice](RELEASE_NOTICE.md) |
[License and attribution](NOTICE-K-Meleon-R.md)

## Features and version changes

The shell provides tabs (including private tabs), an editable GX-style speed
dial, Dark/Light/Sand/Sky/System themes, Bing/Google/Copilot/Gemini search,
Copilot/Gemini query shortcuts, fullscreen video, zoom, autofill, password
saving and renderer reload handling.

- **r15 / 8.7.0 (current):** query-search and general bug-fix release:
  Ask Gemini/Ask Copilot always send the query (dotted single words are no
  longer treated as URLs) and the start page gains a Gemini (Google AI Mode)
  provider. Provider selection is synchronized with the native toolbar.
  Address-bar questions containing URLs are correctly searched; standalone
  website addresses still navigate normally. Copilot shortcuts consistently
  use Copilot Search in Bing, and start-page searches are no longer truncated
  at 256 characters. Unfinished address-bar input is preserved during
  background navigation, native Ask buttons retain private-tab mode, and
  portable-package contents match the documentation.
- **r14 / 8.6.0:** Microsoft Defender integration (SmartScreen on, attachment
  service + AMSI download scanning, Security Center watchdog), Google Safe
  Browsing lookups, anti-screencap shield, `javascript:`/`data:`/`vbscript:`
  blocking, private tabs, Balanced tracking prevention, Copilot/Gemini query
  URLs, installer scripts run with `RemoteSigned`.
- **r13 / 8.5.0:** theme/pointer download/navigation blocking, `ms-settings:`
  blocking, sound-scheme policy and installer GEM OS hardening.
- **r12 / 8.4.0:** page-level system-pointer guard, runtime cursor validation,
  process hardening and elevated cursor lockdown.
- **r11 / 8.3.0:** native bridge hidden from ordinary pages, retained only at
  the local start page for theme synchronization. This avoids web apps
  mistaking this browser for their own desktop host.
- **r10 / 8.2.0:** WebView2 autoplay/fullscreen, crash/unresponsive recovery,
  browser settings and attribution updates.
- **r9 / 8.1.0:** Opera/GX dials and five synchronized themes.

## Build

Requires the .NET 8 SDK, Windows x64 and
[NSIS](https://nsis.sourceforge.io/).

From the repository root:

```powershell
dotnet test kmr-shell.tests\KMeleonR.Search.Tests.csproj -c Release
node --test kmr-shell.tests\start-page.test.cjs
dotnet publish kmr-shell\kmr-shell.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:DebugType=None -o kmr-shell\publish
Copy-Item kmr-shell\NOTICE-K-Meleon-R.md, kmr-shell\README.md kmr-shell\publish
& "C:\Program Files (x86)\NSIS\makensis.exe" /DVERSION=r15 kmr-shell\setup.nsi
Copy-Item kmr-shell\cursor_lockdown.ps1, kmr-shell\harden_against_gem_os.ps1 kmr-shell\publish
Compress-Archive kmr-shell\publish\* kmr-shell\K-Meleon-R-r15-x64-portable.zip
```

Output: `K-Meleon-R-r15-x64-setup.exe`. The project version is `8.7.0`;
the release tag is `v8.7.0-r15`.
Both packages include this README and `RELEASE_NOTICE.md`. Published checksums
are kept on the release page, outside the packages they verify.
Run each command only after the previous one succeeds. Search tests require
Node.js 22+ as well as the .NET SDK. They exercise query encoding, provider
selection and URL routing without launching the browser or changing Windows
policies. The release workflow runs both suites before packaging.

WebView2 must be available on the target computer. If initialization fails, the
shell offers Microsoft's runtime download page.

## Installation and policy effects

The installer elevates when possible (`RequestExecutionLevel highest`) and
defaults to `%LOCALAPPDATA%\Programs\K-Meleon-R`. It shows a license/security
notices page (.NET MIT tribute, SmartScreen/administrator warning) and carries
imortalimp.nl publisher metadata. With administrator rights it runs both
`cursor_lockdown.ps1` and `harden_against_gem_os.ps1 -Apply`, affecting
Windows settings/local profiles; without them the hardening steps are skipped
and the scripts remain installed for later elevated use.
Quarantine is not enabled by the installer.

The runtime guard accepts empty pointer slots or valid system `.cur`/`.ani`
files under `%WINDIR%\Cursors`, resets unapproved paths and protects pointers
against page CSS/JavaScript replacement. It runs for each browser tab.
Theme/pointer files and `ms-settings:` are blocked by the WebView2 guard.
The installer runs the hardening scripts with `-ExecutionPolicy RemoteSigned`.

Per-tab security guards (r14): `DefenderGuard` (SmartScreen, attachment
service + AMSI download scanning, script-scheme navigation blocking),
`SafeBrowsingGuard` (Google Safe Browsing v4, key required) and
`CaptureGuard` (window capture exclusion, page screen-capture blocking).
The Security Center watchdog warns when real-time antivirus protection is off.

Logs and backups:

- `%LOCALAPPDATA%\K-Meleon-R\cursor-guard.log`
- `%LOCALAPPDATA%\K-Meleon-R\defender-guard.log`
- `%ProgramData%\GEM-OS-HARDENING.log`
- `%ProgramData%\GEM-OS-HARDENING-BACKUP\<timestamp>\previous-values.txt`
- `%ProgramData%\AI-WARP-CURSOR-LOCKDOWN.log`
- `%ProgramData%\AI-WARP-cursor-backup_<timestamp>\`

The `AI-WARP` names are the actual shared cursor-script output names, not
K-Meleon-R-specific aliases. Backups require manual review/restoration;
uninstalling the browser does not automatically reverse policy changes.
Do not run apply/quarantine merely to test documentation.

## Shortcuts, profile and debugging

Ctrl+T creates a tab; Ctrl+Shift+N creates a private (in-memory profile) tab;
Ctrl+W closes a tab; Ctrl+Tab and Ctrl+Shift+Tab navigate tabs.
Ctrl+L or F6 focuses the address bar; F5 reloads; Alt+Home opens the start page.
WebView2 browser accelerators and F12 developer tools are enabled.

Settings/profile use `%LOCALAPPDATA%\K-Meleon-R`.
Set `KMR_DEBUG_PORT` only when intentionally enabling Chromium remote debugging.
Set `gsbApiKey` in settings.json (or `KMR_GSB_API_KEY`) to enable Google Safe
Browsing lookups. Ordinary pages cannot access the native host bridge;
`https://kmr.start/index.html` retains it for theme/provider synchronization.
Only submitted searches are sent, and only to the selected provider. No
query text passes through the native host bridge. Ask buttons override the
destination for one submission and keep native private tabs private.

SmartScreen reputation checking is forced on and silently re-asserted on every
tab each minute while the browser runs; tracking prevention defaults to
Balanced (`tracking` setting: `none`/`balanced`/`strict`). The capture shield
is on by default, has no UI and hides the window from the user's own
screenshots too - set `"shield": "off"` in settings.json to capture.
Clipboard-read requests are
automatically allowed and autoplay is enabled. External-service availability
remains outside the shell's control.

## Validation and limits

The r15 query-routing and start-page regression suites pass, and the Windows
release build is verified locally. The r14
shell was smoke-tested (launch, window, clean shutdown). Download scanning and the watchdog depend on the installed
antivirus and Security Center (absent on Windows Server); the capture shield
needs Windows 10 2004+. Machine-policy `-Apply` has not been validated
end-to-end on a real machine. An administrator can undo policies; unsigned
installers may trigger SmartScreen, and a clean targeted scan is not proof of
machine safety.
