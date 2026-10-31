# K-Meleon-R

A small, fast, standalone Windows browser in the spirit of K-Meleon, with an
Opera GX-style speed-dial start page, tabs, a Dark / Light / Sand / Sky / System theme, Opera and Opera GX quick dials, and
Bing, Google, Copilot and Gemini hooks. It renders with the Microsoft Edge
WebView2 runtime that ships with Windows 10/11.

Download the installer or portable zip from the
[Releases](https://github.com/TheImortalimp/K-Meleon-R/releases) page (latest: r12).
The code is in [kmr-shell/](kmr-shell). See [kmr-shell/README.md](kmr-shell/README.md)
for features and shortcuts, and [NOTICE-K-Meleon-R.md](NOTICE-K-Meleon-R.md) for credits.

The older Gecko-based builds (r1 to r5) and the original K-Meleon sources are kept
in this repository for reference.

River Lyle Reuveni (TheImortalimp), AI & .co, with GitHub Copilot.

## Changelog

### r12 (8.4.0)
- System-cursor-only. Web pages can no longer replace the mouse pointer (CSS `cursor:url()`, inline styles, shadow DOM, JS style APIs); the Windows system pointers are always shown.
- Launch guard: every pointer slot in `HKCU\Control Panel\Cursors` must be empty (built-in default) or a `.cur`/`.ani` under `%WINDIR%\Cursors`. Rogue theme cursors (e.g. a GEM-style set) are reset at launch and again on every change. Paths are normalised first (8.3 short names, `..`, UNC/device paths, alternate streams, symlinks and junctions are rejected).
- Process hardening: legacy extension points (AppInit/global hook DLLs, legacy IMEs) and remote or low-integrity image loads are disabled.
- Installer now runs elevated (a standard UAC prompt) and runs `cursor_lockdown.ps1` to lock theme and Settings pointer changes for all local profiles. Previous values are backed up under `%ProgramData%`.
- Log: `%LOCALAPPDATA%\K-Meleon-R\cursor-guard.log`.
### r11 (8.3.0)
- Fixed web apps that detect a native WebView2 host: the AI WARP web edition could not play YouTube in K-Meleon-R. The host bridge is now hidden from ordinary web pages; only the start page keeps it for theme syncing. Verified: the YouTube player reports playing.
- Tracking prevention is off so CDN scripts and embedded players can use storage.
- Optional KMR_DEBUG_PORT environment variable enables remote debugging.

### r10 (8.2.0)
- Better WebView2 handling: autoplay allowed for embedded players (YouTube, Spotify), true fullscreen for video (tabs and toolbar hide), automatic reload after a renderer crash or hang, autofill, password save, zoom, swipe navigation and F12 DevTools.
- NOTICE now carries the licence wording published at imortalimp.nl.
### r9 (8.1.0)
- New Opera and Opera GX quick-dial tiles after the five curated tiles (added once to existing dials).
- Light mode reworked into three themes: Light, Sand and Sky, plus System. The Theme button cycles them.
- Toolbar, tabs and start page now share one theme (synced both ways); the WebView2 background and colour scheme follow it.
- Original SVG icons for the Opera tiles (not official logos).

### r8
Final release of the previous line.

## Install note
The installer is unsigned, so Windows SmartScreen may warn. Choose More info, then Run anyway. SHA-256 hashes are in each release's notes.

## r13 hardening
- **Theme/pointer package blocking:** the embedded WebView2 now cancels downloads and navigations of .theme, .themepack, .deskthemepack, .cur and .ani files (trailing dot/space tricks included) and refuses ms-settings: navigation. External links open only for http/https.
- **Runtime policy:** the guard also sets NoChangingSoundScheme alongside NoChangingMousePointers.
- **Installer:** ships and runs harden_against_gem_os.ps1 -Apply (see the GEM-OS repo). Quarantine of user files stays opt-in.
- **Limits:** an administrator can undo the policies; installers are unsigned so SmartScreen warns (code signing is the real fix); a clean scan is not proof of safety. -Apply was not run end to end on a real machine.
