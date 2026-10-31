# K-Meleon-R shell r13 (8.5.0)

The current Windows browser is a .NET 8 WinForms application using Microsoft
Edge WebView2. It reuses the K-Meleon-R start-page design, not the legacy
Gecko/Goanna C++ implementation. Pale Moon is not required.

[Project README](../README.md) | [About](../ABOUT.md) |
[r13 release notes](../RELEASE_NOTES_r13.md) |
[License and attribution](NOTICE-K-Meleon-R.md)

## Features and version changes

The shell provides tabs, an editable GX-style speed dial, Dark/Light/Sand/Sky/
System themes, Bing/Google/Copilot Search, Copilot/Gemini web shortcuts,
fullscreen video, zoom, autofill, password saving and renderer reload handling.

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

Output: `K-Meleon-R-r13-x64-setup.exe`. The project version is `8.5.0`;
the release tag is `v8.5.0-r13`. The current GitHub release supplies an installer,
not an r13 portable ZIP.

WebView2 must be available on the target computer. If initialization fails, the
shell offers Microsoft's runtime download page.

## Installation and policy effects

The installer requests administrator elevation and defaults to
`%LOCALAPPDATA%\Programs\K-Meleon-R`. It runs both `cursor_lockdown.ps1` and
`harden_against_gem_os.ps1 -Apply`, affecting Windows settings/local profiles.
Quarantine is not enabled by the installer.

The runtime guard accepts empty pointer slots or valid system `.cur`/`.ani`
files under `%WINDIR%\Cursors`, resets unapproved paths and protects pointers
against page CSS/JavaScript replacement. It runs for each browser tab.
Theme/pointer files and `ms-settings:` are blocked by the WebView2 guard.

Logs and backups:

- `%LOCALAPPDATA%\K-Meleon-R\cursor-guard.log`
- `%ProgramData%\GEM-OS-HARDENING.log`
- `%ProgramData%\GEM-OS-HARDENING-BACKUP\<timestamp>\previous-values.txt`
- `%ProgramData%\AI-WARP-CURSOR-LOCKDOWN.log`
- `%ProgramData%\AI-WARP-cursor-backup_<timestamp>\`

The `AI-WARP` names are the actual shared cursor-script output names, not
K-Meleon-R-specific aliases. Backups require manual review/restoration;
uninstalling the browser does not automatically reverse policy changes.
Do not run apply/quarantine merely to test documentation.

## Shortcuts, profile and debugging

Ctrl+T creates a tab; Ctrl+W closes it; Ctrl+Tab and Ctrl+Shift+Tab navigate tabs.
Ctrl+L or F6 focuses the address bar; F5 reloads; Alt+Home opens the start page.
WebView2 browser accelerators and F12 developer tools are enabled.

Settings/profile use `%LOCALAPPDATA%\K-Meleon-R`.
Set `KMR_DEBUG_PORT` only when intentionally enabling Chromium remote debugging.
Ordinary pages cannot access the native host bridge; `https://kmr.start/index.html`
retains it for theme synchronization.

The current host disables tracking prevention and reputation checking and
automatically allows clipboard-read requests. Autoplay is enabled. These
choices favor compatibility, not strict privacy isolation; external-service
availability remains outside the shell's control.

## Validation and limits

This documentation refresh was checked against r13 source/version metadata and
the existing GitHub asset checksum. No executable was rebuilt or replaced.
Machine-policy `-Apply` has not been validated end-to-end on a real machine.
An administrator can undo policies; unsigned installers may trigger SmartScreen,
and a clean targeted scan is not proof of machine safety.
