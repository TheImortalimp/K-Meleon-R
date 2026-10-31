# About K-Meleon-R r13 (8.5.0)

K-Meleon-R is a small standalone Windows browser inspired by K-Meleon, with
a GX-style speed-dial start page, tabs and synchronized Dark, Light, Sand,
Sky and System themes. The current browser uses Microsoft Edge WebView2
through a .NET 8 WinForms shell.

It offers Bing, Google and Copilot Search, plus Copilot/Gemini web shortcuts.
These buttons open external services; they are not local AI models.
Legacy K-Meleon/Gecko/Goanna sources remain in the repository for reference.

## Current version

**r13 / 8.5.0**, tag **v8.5.0-r13**.

r13 adds theme/pointer package download and navigation restrictions,
`ms-settings:` blocking, `NoChangingSoundScheme` policy and elevated installer
hardening. It retains r12 system-pointer protection and r11's native-bridge
compatibility fix for ordinary web pages.

The documentation refresh does not introduce a new binary version. The
existing [r13 installer](https://github.com/TheImortalimp/K-Meleon-R/releases/tag/v8.5.0-r13)
and checksum remain unchanged.

## Protection boundaries

The Windows guard prevents custom web pointer replacement and validates active
pointer paths against the system cursor directory. The installer requires UAC
elevation and applies cursor/theme/sound policies across local profiles.
Quarantine is opt-in, not part of normal installation.

These are targeted theme/cursor-takeover defenses, not a malware scanner, sandbox
certification or general security guarantee. Administrators can change policies.
The machine-policy apply path was not validated end-to-end on a real machine.
The installer is unsigned; verify the published checksum and source before
making a SmartScreen/UAC decision.

Backups/log locations are documented in [shell documentation](kmr-shell/README.md).
Uninstall removes app files/shortcuts, not an automatic restoration of Windows
policy changes.

## Privacy and compatibility choices

The host currently disables tracking prevention and reputation checking,
allows autoplay and automatically approves clipboard-read requests. These
choices favor web-app/embedded-player compatibility and are not a hardened
privacy configuration. Websites, sign-in, DRM and codecs can still be unavailable.

The native host bridge is hidden from ordinary web pages and retained only
for local start-page theme syncing. Settings/profile are local under
`%LOCALAPPDATA%\K-Meleon-R`; optional `KMR_DEBUG_PORT` enables remote debugging.

## Credits and license

River Lyle Reuveni (TheImortalimp), AI & .co, with GitHub Copilot.
[NOTICE-K-Meleon-R.md](NOTICE-K-Meleon-R.md) contains the published attribution
and license wording. Legacy K-Meleon, Goanna/Pale Moon, Babel and dependencies
retain their own licenses.

[README](README.md) | [Release notes](RELEASE_NOTES_r13.md) |
[Description](DESCRIPTION.md)
