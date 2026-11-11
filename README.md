# K-Meleon-R r18 (8.8.2)

**Current/latest release: r18 (8.8.2).**
This is a **patch release** that fixes the bundled SHODAN avatar (face) not
rendering in the shipped single-file build, superseding r17 and earlier
releases. [Always get the latest release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest).

**A small standalone Windows browser with a GX-style start page, tabs,
five synchronized themes, Microsoft Edge WebView2 rendering, a bundled SHODAN
voice-AI side pane and layered Defender/Safe Browsing/anti-screencap hardening.**

## Download and install

[Download the r18 Windows x64 installer](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.2-r18/K-Meleon-R-r18-x64-setup.exe)
| [r18 portable ZIP](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.2-r18/K-Meleon-R-r18-x64-portable.zip)
| [Release notes](RELEASE_NOTES_r18.md)
| [Release notice](kmr-shell/RELEASE_NOTICE.md)
| [About](ABOUT.md)

- Requires Windows 10/11 x64 and the Microsoft Edge WebView2 Runtime.
- The installer defaults to `%LOCALAPPDATA%\Programs\K-Meleon-R`.
- **UAC elevation is optional:** with administrator rights the installer
  applies Windows pointer/theme/sound policy hardening (including local
  profiles) and registers a hidden scheduled task that runs the GEM-OS lockdown
  engine silently every hour ([protection boundaries](ABOUT.md#protection-boundaries));
  without elevation the browser installs per-user and those steps are skipped.
- The installer is unsigned and may trigger SmartScreen. Verify its hash and
  source before deciding whether to run it; a hash is not a safety certificate.
- The portable ZIP runs without installation; the Windows hardening scripts and
  the hourly-task / flint provisioning scripts are included but must be run
  manually (elevated) if wanted.

SHA-256 (r18): published with the [GitHub release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest).

## What's new in r18

A patch release that fixes the bundled SHODAN avatar (face) not rendering in the
shipped build: the single-file publish silently dropped `shodan/avatar.png`, so
the pane's `kmr.shodan` virtual host returned 404 for the face image and the
canvas portrait never became ready. The SHODAN pane assets are now marked
`ExcludeFromSingleFile` so every file ships on disk, and the release workflow
fails fast if the avatar is missing. See the [release notes](RELEASE_NOTES_r18.md).

## What's new in r17

A patch release that makes the SHODAN side-pane window modes coherent and
exception-safe: minimize (to a rail when docked, or the window when floating),
expand width, fullscreen (Esc to exit), float into a real window and dock back,
and close. The float/dock/close paths no longer dispose the live SHODAN WebView2
before reparenting (which previously threw and made the avatar/face appear to
fail), and closing the float window via the OS button now docks the pane back
with its session intact. See the [release notes](RELEASE_NOTES_r17.md).

## What's new in r16

- **.NET 10 (LTS).** The shell and tests moved off .NET 8 (end of support
  10 November 2026) with no feature or UI change.
- **SHODAN side pane.** A right-docked, resizable pane (toolbar **SHODAN** button
  or Ctrl+Shift+S) hosts the bundled SHODAN voice AI; it expands to full width
  and pops out into a floating window. SHODAN's merged **flint:40b** neural link
  is her default conversational and monologue voice, with CORTEX as her soul and
  instant fallback; `install-flint.ps1` provisions the model on loopback.
- **Hourly silent hardening.** An elevated install runs the bundled GEM-OS
  lockdown engine silently every hour via a hidden, highest-privilege scheduled
  task that is removed on uninstall.
- **Hardened pop-up handler.** Pop-ups load their content but never silently:
  verified-malicious targets are refused and everything else opens in its own
  guarded "allowed pop-up" window with an "Open as tab" button.

See the [release notes](RELEASE_NOTES_r16.md).

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
- **Ask Copilot** opens Copilot Search in Bing with your query; **Ask Gemini** opens
  Google AI Mode with your query (or the Gemini app without one). These are
  external web services.
- Tabs (including private tabs), video fullscreen, browser zoom, autofill,
  password saving, developer tools and renderer-crash/unresponsive reload
  handling.
- Ordinary web pages do not receive the native host bridge; the local start
  page retains it for theme and search-provider synchronization.

Website availability, sign-in, codecs and runtime behavior can change.
Compatibility is not guaranteed for every site.

## Search and AI query routing

- Choose **Search: Gemini** for Google AI Mode or **Search: Copilot** for
  Copilot Search in Bing. The choice stays synchronized between the toolbar
  and all local start pages and is remembered for the next launch.
- In the address bar, Enter opens a website address normally. Other text goes
  in full to the selected provider, including questions containing a URL,
  Unicode and punctuation. A dotted hostname such as `foo.bar` is still an
  address; use an **Ask** button to search for it instead.
- The start-page search field always searches with its selected provider.
  **Ask Gemini** and **Ask Copilot** explicitly send the current field text to
  that service, including a URL, without changing the default provider.
  Native Ask buttons preserve private-tab mode.
- Only submission sends the query. The browser does not stream your typing
  or send each search to both AI services. Queries are URL-encoded into the
  selected service's search URL; they can appear in that service's history.
  Neither service receives access to browser controls or other tabs.
- Gemini here means **Google AI Mode**, not a Gemini chat/API integration.
  An empty Ask Gemini opens the Gemini app; an empty Ask Copilot opens Copilot
  Search. Availability and account requirements are controlled by those sites.

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

The current implementation is [kmr-shell/](kmr-shell/), a .NET 10
WinForms/WebView2 shell; it is not the old Gecko/Goanna C++ browser.
See [shell documentation](kmr-shell/README.md) for build commands and policy logs.
The older r1-r5 builds and original K-Meleon sources remain for reference.

## Version history

| Release | Changes |
|---------|---------|
| **r18 / 8.8.2 (current)** | Patch: fixes the bundled SHODAN avatar (face) not rendering in the shipped single-file build (`shodan/avatar.png` was dropped by `PublishSingleFile`); assets now ship via `ExcludeFromSingleFile` |
| r17 / 8.8.1 | Patch: SHODAN side-pane window modes (minimize / expand / fullscreen / float / dock / close) made coherent and exception-safe; float/dock/close no longer dispose the live pane (which had made the avatar appear to fail) |
| r16 / 8.8.0 | .NET 10 (LTS); bundled SHODAN voice-AI side pane (dock/expand/float) with a merged flint:40b neural link; hourly silent GEM-OS hardening task; hardened pop-up handler that blocks verified-malicious targets and opens the rest in a guarded window |
| r15 / 8.7.0 | Query-search and general bug-fix build: complete query routing, synchronized providers, preserved input/private tabs and corrected packaging |
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
