# K-Meleon-R r16 (8.8.0) - release notice

**.NET 10, SHODAN side pane, hourly hardening and hardened pop-ups.**
This is the current stable release, superseding r15 and earlier builds.
Release tag: **v8.8.0-r16**.

## About this build

K-Meleon-R is a standalone Windows x64 browser using **.NET 10** and Microsoft
Edge WebView2, with tabs, private tabs, a GX-style start page, five themes, and
a layered, self-healing security stack (Cursor/Defender/SafeBrowsing/Capture
guards). Windows 10/11 x64 and the Microsoft Edge WebView2 Runtime are required.
The release is available as an EXE installer and a portable ZIP.

## What's new in r16

- **.NET 10 (LTS).** The shell and tests moved from .NET 8 (end of support
  10 November 2026) to the current .NET LTS, with no change to features or UI.
- **SHODAN side pane.** A right-docked, resizable pane (toolbar **SHODAN** button
  or Ctrl+Shift+S) hosts the bundled SHODAN voice AI. It can be expanded to full
  width and popped out into a floating window, then docked back. It is bundled
  offline and served from the local `kmr.shodan` virtual host.
- **Merged flint:40b brain.** SHODAN's default conversational and monologue
  voice is the `flint:40b` local LLM, with CORTEX kept as her soul and instant
  fallback. `install-flint.ps1` pulls the model and allows the pane origin; the
  link only ever contacts loopback.
- **Hourly silent hardening.** An elevated install registers a hidden scheduled
  task that runs the bundled GEM-OS lockdown engine silently every hour with the
  highest privileges and no UAC prompt. Uninstall removes the task.
- **Hardened pop-up handler.** Pop-ups load their content but never silently:
  verified-malicious targets are refused, and everything else opens in its own
  guarded window with an "allowed pop-up" notice and an "Open as tab" button.

## Privacy and installation boundaries

The SHODAN neural link and all speech stay on your computer; WHITE ICE only
contacts loopback endpoints and CORTEX works fully offline. Pop-up malicious
verification uses Google Safe Browsing only when an API key is configured.

The installer is unsigned and may trigger SmartScreen. Verify its checksum and
source before running it. Elevated installation applies Windows pointer/theme/
sound policies (not undone by uninstall) and the hourly hardening task (removed
by uninstall). Portable packages never change machine policies automatically.
These defenses and package checks are not a general safety guarantee.

## Downloads, checksums and documentation

- [This release and SHA-256 checksums](https://github.com/TheImortalimp/K-Meleon-R/releases/tag/v8.8.0-r16)
- [Latest available release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest)
- [Project About](https://github.com/TheImortalimp/K-Meleon-R/blob/v8.8.0-r16/ABOUT.md)
- [Browser documentation](README.md)
- [License and attribution](NOTICE-K-Meleon-R.md)
