# K-Meleon-R r13 (8.5.0)

**A small standalone Windows browser with a GX-style start page, tabs,
five synchronized themes and Microsoft Edge WebView2 rendering.**

## Download and install

[Download the r13 Windows x64 installer](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.5.0-r13/K-Meleon-R-r13-x64-setup.exe)
| [Release notes](RELEASE_NOTES_r13.md)
| [About](ABOUT.md)

- Requires Windows 10/11 x64 and the Microsoft Edge WebView2 Runtime.
- The installer defaults to `%LOCALAPPDATA%\Programs\K-Meleon-R`.
- **Approve UAC elevation:** installation applies Windows pointer/theme/sound
  policy hardening, including local profiles. Review [protection boundaries](ABOUT.md#protection-boundaries).
- The installer is unsigned and may trigger SmartScreen. Verify its hash and
  source before deciding whether to run it; a hash is not a safety certificate.
- r13 currently provides an installer only; earlier releases may have portable
  archives. Do not assume an older portable download contains r13 fixes.

Installer SHA-256:

```text
5CD14BA6F6D297B9E2D492DF69D04477699A8A46F581FE5AD188DA8F5EFE9EFB
```

## What's new in r13

- Blocks theme/pointer package downloads and navigation for `.theme`,
  `.themepack`, `.deskthemepack`, `.cur` and `.ani`, including trailing-dot/space
  variants; blocks `ms-settings:` navigation.
- Adds `NoChangingSoundScheme` alongside `NoChangingMousePointers`.
- Ships and runs `harden_against_gem_os.ps1 -Apply`; quarantine remains opt-in.
- Retains r12 system-pointer guards and process hardening, plus the r11
  native-host bridge fix used by embedded-player web apps.

These are targeted defenses against theme/cursor takeover, not general
anti-malware protection. Administrators can undo policies. The machine-policy
apply path was not validated end-to-end on a real machine.

## Browser features

- GX-style speed dial: MSN, Google, YouTube, Bandcamp, Spotify, Opera and
  Opera GX, plus editable custom tiles and thumbnails.
- Dark, Light, Sand, Sky and System themes, synchronized across start page,
  tabs, toolbar and WebView2 background.
- Address/search bar with Bing, Google and Copilot Search providers.
- **Ask Copilot** opens Copilot Search; **Ask Gemini** copies the text to the
  clipboard and opens Gemini for pasting. These are external web services.
- Tabs, video fullscreen, browser zoom, autofill, password saving, developer
  tools and renderer-crash/unresponsive reload handling.
- Ordinary web pages do not receive the native host bridge; the local start
  page retains it for theme synchronization.

Website availability, sign-in, codecs and runtime behavior can change.
Compatibility is not guaranteed for every site.

## Shortcuts and settings

| Shortcut | Action |
|----------|--------|
| Ctrl+T / Ctrl+W | New tab / close tab |
| Ctrl+Tab / Ctrl+Shift+Tab | Next / previous tab |
| Ctrl+L / F6 | Focus address bar |
| F5 | Reload |
| Alt+Home | Start page |
| F12 | WebView2 developer tools |

Settings and browser profile live under `%LOCALAPPDATA%\K-Meleon-R`.
The cursor runtime log is `cursor-guard.log` in that folder.
Optional `KMR_DEBUG_PORT` enables local Chromium remote debugging; leave it
unset for normal use.

Tracking prevention and reputation checking are disabled in the current host.
This favors embedded-player compatibility but is not a privacy-hardened
configuration. Clipboard-read requests are automatically allowed.
See [About](ABOUT.md) for these boundaries.

## Source and build

The current implementation is [kmr-shell/](kmr-shell/), a .NET 8
WinForms/WebView2 shell; it is not the old Gecko/Goanna C++ browser.
See [shell documentation](kmr-shell/README.md) for build commands and policy logs.
The older r1-r5 builds and original K-Meleon sources remain for reference.

This update refreshes r13 documentation and GitHub About only. It does not
replace the existing r13 installer or claim new runtime testing.

## Version history

| Release | Changes |
|---------|---------|
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
