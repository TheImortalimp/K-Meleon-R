# About K-Meleon-R r15 (8.7.0)

K-Meleon-R is a small standalone Windows browser inspired by K-Meleon, with
a GX-style speed-dial start page, tabs and synchronized Dark, Light, Sand,
Sky and System themes. The current browser uses Microsoft Edge WebView2
through a .NET 8 WinForms shell.

It offers Bing, Google, Copilot and Gemini (Google AI Mode) search, plus
Copilot/Gemini query shortcuts. These buttons open external services; they are
not local AI models. Legacy K-Meleon/Gecko/Goanna sources remain in the
repository for reference.

## Current version

**r14 / 8.6.0**, tag
**[v8.6.0-r14](https://github.com/TheImortalimp/K-Meleon-R/releases/tag/v8.6.0-r14)**
(installer and portable ZIP).

r14 integrates the Microsoft and Google protection layers directly into every
tab: SmartScreen reputation checking, Attachment Execution Service + AMSI
download scanning through the installed antivirus (Microsoft Defender by
default), a Security Center watchdog, optional Google Safe Browsing lookups,
an anti-screencap window shield, a script-scheme phishing filter and private
tabs. It retains the r13/r12 theme/cursor takeover defenses and r11's
native-bridge compatibility fix.

## Protection boundaries

The Windows guard prevents custom web pointer replacement and validates active
pointer paths against the system cursor directory. The installer requires UAC
elevation and applies cursor/theme/sound policies across local profiles.
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
for local start-page theme syncing. Settings/profile are local under
`%LOCALAPPDATA%\K-Meleon-R`; optional `KMR_DEBUG_PORT` enables remote debugging.

## Credits and license

River Lyle Reuveni (TheImortalimp), AI & .co, with GitHub Copilot.
[NOTICE-K-Meleon-R.md](NOTICE-K-Meleon-R.md) contains the published attribution
and license wording. Legacy K-Meleon, Goanna/Pale Moon, Babel and dependencies
retain their own licenses.

[README](README.md) | [Release notes](RELEASE_NOTES_r14.md) |
[Description](DESCRIPTION.md)
