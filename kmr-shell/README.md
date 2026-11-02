# K-Meleon-R shell r15 (8.7.0)

The current Windows browser is a .NET 8 WinForms application using Microsoft
Edge WebView2. It reuses the K-Meleon-R start-page design, not the legacy
Gecko/Goanna C++ implementation. Pale Moon is not required.

[Project README](../README.md) | [About](../ABOUT.md) |
[r14 release notes](../RELEASE_NOTES_r15.md) |
[License and attribution](NOTICE-K-Meleon-R.md)

## Features and version changes

The shell provides tabs (including private tabs), an editable GX-style speed
dial, Dark/Light/Sand/Sky/System themes, Bing/Google/Copilot/Gemini search,
Copilot/Gemini query shortcuts, fullscreen video, zoom, autofill, password
saving and renderer reload handling.

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
Set-Location kmr-shell
dotnet publish -c Release -r win-x64 --self-contained true -o publish
& "C:\Program Files (x86)\NSIS\makensis.exe" setup.nsi
```

Output: `K-Meleon-R-r15-x64-setup.exe`. The project version is `8.7.0`;
the release tag is `v8.7.0-r15`.

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
`https://kmr.start/index.html` retains it for theme synchronization.

SmartScreen reputation checking is forced on and silently re-asserted on every
tab each minute while the browser runs; tracking prevention defaults to
Balanced (`tracking` setting: `none`/`balanced`/`strict`). The capture shield
is on by default, has no UI and hides the window from the user's own
screenshots too - set `"shield": "off"` in settings.json to capture.
Clipboard-read requests are
automatically allowed and autoplay is enabled. External-service availability
remains outside the shell's control.

## Validation and limits

The r14 code builds cleanly and the shell was smoke-tested (launch, window,
clean shutdown). Download scanning and the watchdog depend on the installed
antivirus and Security Center (absent on Windows Server); the capture shield
needs Windows 10 2004+. Machine-policy `-Apply` has not been validated
end-to-end on a real machine. An administrator can undo policies; unsigned
installers may trigger SmartScreen, and a clean targeted scan is not proof of
machine safety.
