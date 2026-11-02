# K-Meleon-R r15 (8.7.0)

**A small standalone Windows browser with a GX-style start page, tabs,
five synchronized themes, Microsoft Edge WebView2 rendering and layered
Defender/Safe Browsing/anti-screencap hardening.**

## Download and install

[Download the r14 Windows x64 installer](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.6.0-r14/K-Meleon-R-r14-x64-setup.exe)
| [r14 portable ZIP](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.6.0-r14/K-Meleon-R-r14-x64-portable.zip)
| [Release notes](RELEASE_NOTES_r14.md)
| [About](ABOUT.md)

- Requires Windows 10/11 x64 and the Microsoft Edge WebView2 Runtime.
- The installer defaults to `%LOCALAPPDATA%\Programs\K-Meleon-R`.
- **UAC elevation is optional:** with administrator rights the installer
  applies Windows pointer/theme/sound policy hardening, including local
  profiles ([protection boundaries](ABOUT.md#protection-boundaries)); without
  them the browser installs per-user and the hardening steps are skipped.
- The installer is unsigned and may trigger SmartScreen. Verify its hash and
  source before deciding whether to run it; a hash is not a safety certificate.
- The portable ZIP runs without installation; the Windows hardening scripts are
  included but must be run manually (elevated) if wanted.

SHA-256 (r14):

```text
K-Meleon-R-r14-x64-setup.exe    440808FF57BA7DC881CE41719A8E8C2ED3E2B0A48019EE6431A0C18B94D9556E
K-Meleon-R-r14-x64-portable.zip A876BB2810E0A945546D9D3AF940BB87531C210D47B46109D426BC2B8B0F91EE
```

## What's new in r14

- **Microsoft Defender hooks:** SmartScreen reputation checking forced on;
  every completed download is scanned through the Windows Attachment Execution
  Service (Mark-of-the-Web + Defender) and AMSI, with flagged files removed;
  a Security Center watchdog warns when real-time antivirus protection is off.
- **Google Safe Browsing:** navigations are checked against the Safe Browsing
  v4 blocklists and flagged pages replaced by a local warning page (needs a
  free API key - `gsbApiKey` setting or `KMR_GSB_API_KEY`).
- **Anti-screencap shield (on by default, no UI):** the window is excluded from
  screenshots/recording/remote-viewing tools, and web pages can never start a
  screen capture. Runs invisibly; `"shield": "off"` in settings.json disables it.
- **Phishing filter:** `javascript:`/`data:`/`vbscript:` navigations blocked,
  punycode (IDN homograph) hosts logged.
- **Private tabs** (Ctrl+Shift+N, in-memory profile) and tracking prevention
  raised to Balanced by default.
- **Copilot/Gemini:** Ask Copilot passes the query to `copilot.microsoft.com`;
  Ask Gemini opens Google AI Mode with the query; Gemini joins the search
  provider cycle.
- Installer hardening scripts now run with `-ExecutionPolicy RemoteSigned`
  instead of `Bypass`.

These are layered defenses, not general anti-malware protection or a safety
guarantee. Administrators can undo policies.

## Browser features

- GX-style speed dial: MSN, Google, YouTube, Bandcamp, Spotify, Opera and
  Opera GX, plus editable custom tiles and thumbnails.
- Dark, Light, Sand, Sky and System themes, synchronized across start page,
  tabs, toolbar and WebView2 background.
- Address/search bar with Bing, Google, Copilot and Gemini (Google AI Mode)
  search providers.
- **Ask Copilot** opens Microsoft Copilot with your query; **Ask Gemini** opens
  Google AI Mode with your query (or the Gemini app without one). These are
  external web services.
- Tabs (including private tabs), video fullscreen, browser zoom, autofill,
  password saving, developer tools and renderer-crash/unresponsive reload
  handling.
- Ordinary web pages do not receive the native host bridge; the local start
  page retains it for theme synchronization.

Website availability, sign-in, codecs and runtime behavior can change.
Compatibility is not guaranteed for every site.

## Shortcuts and settings

| Shortcut | Action |
|----------|--------|
| Ctrl+T / Ctrl+W | New tab / close tab |
| Ctrl+Shift+N | New private tab |
| Ctrl+Tab / Ctrl+Shift+Tab | Next / previous tab |
| Ctrl+L / F6 | Focus address bar |
| F5 | Reload |
| Alt+Home | Start page |
| F12 | WebView2 developer tools |

Settings and browser profile live under `%LOCALAPPDATA%\K-Meleon-R`.
Runtime logs in that folder: `cursor-guard.log` and `defender-guard.log`.
Optional `KMR_DEBUG_PORT` enables local Chromium remote debugging; leave it
unset for normal use. Optional `gsbApiKey` (settings.json) or
`KMR_GSB_API_KEY` enables Google Safe Browsing lookups.

SmartScreen reputation checking is always on; tracking prevention defaults to
Balanced (`tracking` setting: `none`/`balanced`/`strict`). Clipboard-read
requests are still automatically allowed.
See [About](ABOUT.md) for these boundaries.

## Source and build

The current implementation is [kmr-shell/](kmr-shell/), a .NET 8
WinForms/WebView2 shell; it is not the old Gecko/Goanna C++ browser.
See [shell documentation](kmr-shell/README.md) for build commands and policy logs.
The older r1-r5 builds and original K-Meleon sources remain for reference.

## Version history

| Release | Changes |
|---------|---------|
| r14 / 8.6.0 | Defender download scanning + SmartScreen + AV watchdog, Google Safe Browsing, anti-screencap shield, private tabs, Copilot/Gemini query support |
| r13 / 8.5.0 | Theme/pointer package blocking, sound-scheme policy, elevated GEM OS hardening |
| r12 / 8.4.0 | System-pointer-only page/runtime guard; process hardening and elevated cursor lockdown |
| r11 / 8.3.0 | Hide native host bridge from ordinary pages; embedded-player compatibility; optional debug port |
| r10 / 8.2.0 | WebView2 autoplay/fullscreen, renderer recovery, browser settings and updated attribution |
| r9 / 8.1.0 | Opera/GX dials and synchronized Dark/Light/Sand/Sky/System themes |
| r8 | Final release of the previous line |

## Credits and license

River Lyle Reuveni (TheImortalimp), AI & .co, with GitHub Copilot.
See [NOTICE-K-Meleon-R.md](NOTICE-K-Meleon-R.md) for attribution and the published
license wording. K-Meleon, Goanna/Pale Moon, Babel and other third-party
components retain their own licenses; the shell does not override them.
