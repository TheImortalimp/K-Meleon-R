# K-Meleon-R r15 (8.7.0) - query-search fix and general bug-fix release

**Current/latest stable release - 2 November 2026.**
r15 supersedes r14 and earlier builds. This maintenance release completes
Gemini/Copilot query-search support and fixes related input, provider
synchronization, private-tab and packaging behavior.

## Downloads

- [Windows x64 EXE installer](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.7.0-r15/K-Meleon-R-r15-x64-setup.exe)
- [Windows x64 portable ZIP](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.7.0-r15/K-Meleon-R-r15-x64-portable.zip)
- [Always get the latest release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest)

Both packages include updated documentation and `RELEASE_NOTICE.md`.

## Fixes
- **Ask Gemini / Ask Copilot:** a one-word query containing a dot (e.g. `foo.bar`)
  was treated as a URL and opened the home page. Non-empty queries are now always
  sent as searches.
- **Start page:** Gemini (Google AI Mode, `udm=50`) added to the search provider
  cycle; **Ask Gemini** passes your query in the URL instead of only copying it
  to the clipboard.
- Release packaging builds r15 (was hardcoded to r11).
- **One provider selection:** the toolbar and all local start pages stay in
  sync and remember the chosen provider. Only that provider receives a
  submitted search; typing and other tabs are not shared with AI services.
- **Address-bar routing:** real website addresses still open normally.
  Questions containing URLs, Unicode, and punctuation are sent intact as
  searches. Background navigation no longer replaces unfinished field text.
- **Consistent AI destinations:** Gemini uses Google AI Mode; Copilot uses
  Copilot Search in Bing from both the toolbar and start page. Ask buttons
  explicitly choose the destination without changing the default provider.
  Native Ask buttons retain private-tab mode.
- **Full start-page queries:** removed the 256-character input restriction.
- **Packaging and validation:** native and start-page search regression tests
  run before packaging. The portable ZIP now includes the optional Windows
  hardening scripts described in the documentation.

## Requirements and boundaries

Windows 10/11 x64 and the Microsoft Edge WebView2 Runtime are required.
Gemini here is Google AI Mode, not a Gemini chat/API integration. AI site
availability, responses, and sign-in requirements remain outside the browser's
control. Submitted queries are encoded in the selected service's URL.

The installer is unsigned. Verify its checksum and source before running it.
An elevated install applies Windows policy hardening that uninstall does not
undo; portable policy scripts are optional and are not run automatically.
Release validation does not run the installer or change machine policies.

## Validation

The search fix passed 59 native regression tests and 10 start-page tests.
The Windows x64 release build and NSIS installer compilation succeeded.
Package contents and SHA-256 hashes are checked before publication; these
checks are not a guarantee that all websites or machine-policy behavior work.

## SHA-256

Built from source commit `96928000526538fa6ff58e6366735a3e96ffbdf5`.

```
K-Meleon-R-r15-x64-setup.exe    5BA4A9C5922FC05D2CF77A42CC635E661FA145228A8EE35FEF921F7141265E6B
K-Meleon-R-r15-x64-portable.zip 765FF18706221C9044FC4C91E4242EAF820FD7637EEC2A052D10C9AFC6DB85AA
```
