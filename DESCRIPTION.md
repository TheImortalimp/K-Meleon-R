# K-Meleon-R - Description

## GitHub About

K-Meleon-R r18 (8.8.2) - hardened Windows x64 WebView2 browser (.NET 10) with a bundled SHODAN voice-AI side pane (flint:40b + CORTEX), hourly silent system hardening, a hardened pop-up handler, synchronized Gemini/Copilot search, private tabs and GX-style themes. EXE installer and portable ZIP.

## Summary

K-Meleon-R combines a lightweight WinForms/WebView2 browser shell with editable
speed dials, synchronized themes, browser tabs, private tabs and web search/AI
shortcuts. It runs on Windows 10/11 x64 with Microsoft Edge WebView2.

**r18 (8.8.2) is the current release**, superseding r17 and earlier builds. It
is a patch that fixes the bundled SHODAN avatar (face) not rendering in the
shipped single-file build. It continues the r16-r17 line: **r16 (8.8.0)** moved
the shell to **.NET 10 (LTS)** (.NET 8 reached end of support on 10 November
2026), added the bundled **SHODAN** voice-AI side pane (dock, expand, minimize,
fullscreen and float) with **flint:40b** as SHODAN's default conversational and
monologue voice and CORTEX kept as her soul and fallback, added an hourly silent
GEM-OS hardening task, and added a hardened pop-up handler that refuses
verified-malicious targets and opens the rest in a guarded window; **r17
(8.8.1)** made the pane window modes coherent and exception-safe.

The browser integrates SmartScreen, attachment-service/AMSI download scanning
through the installed antivirus, a Security Center watchdog, optional Google
Safe Browsing lookups, an anti-screencap window shield and a script-scheme
phishing filter, on top of the theme/cursor takeover defenses. It keeps
synchronized Gemini (Google AI Mode)/Copilot Search query routing without
sharing typing or sending searches to both services.
The installer changes Windows settings; it is unsigned, quarantine is opt-in,
and these layered defenses are not a general safety guarantee.

[Latest release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest)
| [Download r18](https://github.com/TheImortalimp/K-Meleon-R/releases/tag/v8.8.2-r18)
| [README](README.md) | [About](ABOUT.md) |
[r18 release notes](RELEASE_NOTES_r18.md) | [Release notice](kmr-shell/RELEASE_NOTICE.md)
