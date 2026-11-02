# K-Meleon-R r15 (8.7.0) - release notice

**Query-search fix and general bug-fix build.**
This is the current stable release as of **2 November 2026**, superseding
r14 and earlier builds. Release tag: **v8.7.0-r15**.

## About this build

K-Meleon-R is a standalone Windows x64 browser using .NET 8 and Microsoft Edge
WebView2, with tabs, private tabs, a GX-style start page and five themes.
Windows 10/11 x64 and the Microsoft Edge WebView2 Runtime are required.
The release is available as an EXE installer and a portable ZIP.

## Query-search and other fixes

- Gemini (Google AI Mode) and Copilot Search in Bing receive the full
  submitted query when selected. The toolbar and local start pages share
  and remember the same provider choice.
- Website addresses still open normally. Questions containing URLs,
  Unicode and punctuation are searched without losing their text.
- Ask Gemini and Ask Copilot explicitly choose that service for the current
  field text, including dotted text or a URL, without changing the default
  provider. Native Ask buttons preserve private-tab mode.
- The start-page search field no longer has a 256-character input limit.
  Background navigation no longer replaces unfinished address-bar text.
- Portable-package contents now include the documented optional policy
  scripts. Both packages include this notice and the updated README.

## Privacy and installation boundaries

Queries are sent only when submitted and only to the selected service.
Typing is not streamed to both AIs, and neither gets access to browser
controls or other tabs. Gemini here means Google AI Mode, not a Gemini
chat/API integration; service availability and sign-in are controlled by
Google and Microsoft.

The installer is unsigned and may trigger SmartScreen. Verify its checksum
and source before running it. Elevated installation applies Windows
pointer/theme/sound policies that uninstall does not automatically undo.
Portable policy scripts are optional and never run automatically.
These defenses and package checks are not a general safety guarantee.

## Downloads, checksums and documentation

- [This release and SHA-256 checksums](https://github.com/TheImortalimp/K-Meleon-R/releases/tag/v8.7.0-r15)
- [Latest available release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest)
- [Project About](https://github.com/TheImortalimp/K-Meleon-R/blob/v8.7.0-r15/ABOUT.md)
- [Browser documentation](README.md)
- [License and attribution](NOTICE-K-Meleon-R.md)
