# About K-Meleon-R r18 (8.8.2)

K-Meleon-R is a small standalone Windows browser inspired by K-Meleon, with
a GX-style speed-dial start page, tabs and synchronized Dark, Light, Sand,
Sky and System themes. The current browser uses Microsoft Edge WebView2
through a .NET 10 WinForms shell, and bundles the SHODAN voice-AI side pane.

It offers Bing, Google, Copilot and Gemini (Google AI Mode) search, plus
Copilot/Gemini query shortcuts. These buttons open external services; they are
not local AI models. The bundled SHODAN pane runs its CORTEX generator and
speech on-device, and its optional flint:40b neural link only ever contacts a
local (loopback) model server. Legacy K-Meleon/Gecko/Goanna sources remain in
the repository for reference.

## Current/latest release

**r18 / 8.8.2** is the current release, a patch that fixes the bundled SHODAN
avatar (face) not rendering in the shipped single-file build: `PublishSingleFile`
silently dropped `shodan/avatar.png`, so the pane's `kmr.shodan` virtual host
returned 404 for the face image. The SHODAN pane assets now ship via
`ExcludeFromSingleFile`, and the release workflow fails fast if the avatar is
missing. Release tag:
**[v8.8.2-r18](https://github.com/TheImortalimp/K-Meleon-R/releases/tag/v8.8.2-r18)**
(Windows x64 EXE installer and portable ZIP).

This continues the r16-r17 line: **r16 / 8.8.0** moved the shell to **.NET 10
(LTS)** (.NET 8 reached end of support on 10 November 2026), added the bundled
**SHODAN** voice-AI side pane (toolbar button or Ctrl+Shift+S) that docks on the
right, expands, and pops out into a floating window; made **flint:40b** SHODAN's
default conversational and monologue voice with CORTEX kept as her soul and
fallback; added an hourly silent GEM-OS hardening task; and added a hardened
pop-up handler that refuses verified-malicious targets and opens the rest in a
guarded window. **r17 / 8.8.1** made the pane window modes (minimize, expand,
fullscreen, float, dock, close) coherent and exception-safe.

[Latest release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest)
| [EXE installer](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.2-r18/K-Meleon-R-r18-x64-setup.exe)
| [Portable ZIP](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.2-r18/K-Meleon-R-r18-x64-portable.zip)
| [Release notice](kmr-shell/RELEASE_NOTICE.md)

r14 introduced the Microsoft and Google protection layers directly into every
tab: SmartScreen reputation checking, Attachment Execution Service + AMSI
download scanning through the installed antivirus (Microsoft Defender by
default), a Security Center watchdog, optional Google Safe Browsing lookups,
an anti-screencap window shield, a script-scheme phishing filter and private
tabs. It retains the r13/r12 theme/cursor takeover defenses and r11's
native-bridge compatibility fix.

## Protection boundaries

The Windows guard prevents custom web pointer replacement and validates active
pointer paths against the system cursor directory. The installer requests UAC
elevation when possible; only elevated installs apply cursor/theme/sound
policies across local profiles.
Quarantine is opt-in, not part of normal installation.

Downloads are screened by SmartScreen, the Windows attachment service and AMSI
through whichever antivirus is registered; navigations can additionally be
checked against Google Safe Browsing when an API key is configured. The
capture shield hides the window from screenshots and recording (including the
user's own) and web pages cannot start screen captures.

These are layered defenses, not a malware scanner replacement, sandbox
certification or general security guarantee. Administrators can change policies.
The machine-policy apply path was not validated end-to-end on a real machine.
The installer is unsigned; verify the published checksum and source before
making a SmartScreen/UAC decision.

Backups/log locations are documented in [shell documentation](kmr-shell/README.md).
Uninstall removes app files/shortcuts, not an automatic restoration of Windows
policy changes.

## Privacy and compatibility choices

SmartScreen reputation checking is always on and tracking prevention defaults
to Balanced. Private tabs (Ctrl+Shift+N) use an in-memory profile. The host
still allows autoplay and automatically approves clipboard-read requests for
web-app/embedded-player compatibility. Websites, sign-in, DRM and codecs can
still be unavailable. Safe Browsing lookups, when enabled, send only the
visited URL to Google.

The native host bridge is hidden from ordinary web pages and retained only
for local start-page theme and provider syncing. Gemini searches use Google
AI Mode; Copilot searches use Copilot Search in Bing. Only the selected
service receives a submitted query, encoded in its search URL. Typing is not
streamed to either service, and neither can read browser controls or other
tabs. Ask buttons explicitly choose the destination for that submission.
Settings/profile are local under
`%LOCALAPPDATA%\K-Meleon-R`; optional `KMR_DEBUG_PORT` enables remote debugging.

## Credits and license

River Lyle Reuveni (TheImortalimp), AI & .co, with GitHub Copilot.
[NOTICE-K-Meleon-R.md](NOTICE-K-Meleon-R.md) contains the published attribution
and license wording. Legacy K-Meleon, Goanna/Pale Moon, Babel and dependencies
retain their own licenses.

[README](README.md) | [Release notes](RELEASE_NOTES_r18.md) |
[Description](DESCRIPTION.md)
