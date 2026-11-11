# K-Meleon-R shell r18 (8.8.2)

**Current release: r18 (8.8.2), a patch that fixes the bundled SHODAN avatar
(face) not rendering in the shipped single-file build.** It supersedes r17 and
earlier versions and is available as a Windows x64 EXE installer and a portable
ZIP.

The current Windows browser is a .NET 10 WinForms application using Microsoft
Edge WebView2. It reuses the K-Meleon-R start-page design, not the legacy
Gecko/Goanna C++ implementation. Pale Moon is not required.

[Project README](https://github.com/TheImortalimp/K-Meleon-R/blob/v8.8.2-r18/README.md) |
[About](https://github.com/TheImortalimp/K-Meleon-R/blob/v8.8.2-r18/ABOUT.md) |
[r18 release notes and checksums](https://github.com/TheImortalimp/K-Meleon-R/releases/tag/v8.8.2-r18) |
[Latest release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest) |
[Included release notice](RELEASE_NOTICE.md) |
[License and attribution](NOTICE-K-Meleon-R.md)

## Features and version changes

The shell provides tabs (including private tabs), an editable GX-style speed
dial, Dark/Light/Sand/Sky/System themes, Bing/Google/Copilot/Gemini search,
Copilot/Gemini query shortcuts, fullscreen video, zoom, autofill, password
saving and renderer reload handling.

- **r18 / 8.8.2 (current):** patch that fixes the bundled SHODAN avatar (face)
  not rendering in the shipped single-file build. `PublishSingleFile` silently
  dropped `shodan/avatar.png`, so the pane's `kmr.shodan` virtual host returned
  404 for the face image; the SHODAN pane assets are now marked
  `ExcludeFromSingleFile` so every file ships on disk.
- **r17 / 8.8.1:** patch that makes the SHODAN side-pane window modes
  (minimize, expand, fullscreen, float, dock, close) coherent and exception-safe.
  The float/dock/close paths no longer dispose the live SHODAN WebView2 before
  reparenting (which previously threw and made the avatar/face appear to fail),
  and closing the float window via the OS button docks the pane back intact.
- **r16 / 8.8.0:** moved to **.NET 10 (LTS)**; added the bundled
  **SHODAN** voice-AI side pane (toolbar button or Ctrl+Shift+S) that docks on
  the right, expands to full width and pops out into a floating window; SHODAN's
  merged **flint:40b** neural link is her default conversational and monologue
  voice with CORTEX kept as her soul and fallback; an elevated install runs the
  GEM-OS lockdown engine silently **every hour** via a hidden scheduled task;
  and a **hardened pop-up handler** refuses verified-malicious targets and opens
  everything else in its own guarded "allowed pop-up" window.
- **r15 / 8.7.0:** query-search and general bug-fix release:
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

Requires the .NET 10 SDK, Node.js 22+, Windows x64 and
[NSIS](https://nsis.sourceforge.io/). The SHODAN side pane is bundled from the
[SHODAN-A.I.-Voice](https://github.com/TheImortalimp/SHODAN-A.I.-Voice) repo: the
release workflow clones it, runs `npm ci && npm run build`, and copies `dist/`
into `kmr-shell/shodan/` before publishing (that folder is git-ignored).

From the repository root:

```powershell
dotnet test kmr-shell.tests\KMeleonR.Search.Tests.csproj -c Release
node --test kmr-shell.tests\start-page.test.cjs
# Bundle the SHODAN pane (optional for a dev build; required for a full release):
git clone --depth 1 https://github.com/TheImortalimp/SHODAN-A.I.-Voice.git shodan-src
pushd shodan-src; npm ci; npm run build; popd
Remove-Item kmr-shell\shodan -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory kmr-shell\shodan | Out-Null
Copy-Item shodan-src\dist\* kmr-shell\shodan -Recurse -Force
dotnet publish kmr-shell\kmr-shell.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:DebugType=None -o kmr-shell\publish
Copy-Item kmr-shell\NOTICE-K-Meleon-R.md, kmr-shell\README.md kmr-shell\publish
& "C:\Program Files (x86)\NSIS\makensis.exe" /DVERSION=r18 kmr-shell\setup.nsi
Copy-Item kmr-shell\cursor_lockdown.ps1, kmr-shell\harden_against_gem_os.ps1, `
  kmr-shell\Hardening_Lockdown_Standalone.bat, kmr-shell\install_hourly_hardening.ps1, `
  kmr-shell\install-flint.ps1 kmr-shell\publish
Compress-Archive kmr-shell\publish\* kmr-shell\K-Meleon-R-r18-x64-portable.zip
```

Output: `K-Meleon-R-r18-x64-setup.exe`. The project version is `8.8.2`;
the release tag is `v8.8.2-r18`.
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
