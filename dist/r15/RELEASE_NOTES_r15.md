# K-Meleon-R r15 (8.7.0) - bugfix release

Completes the Google/Gemini search query support that was only partly done in r14.

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

## SHA-256
```
K-Meleon-R-r15-x64-setup.exe    CCA7FFB5F8337EE22E17185B1C3E719D0C0E703B2A16670C8A49DE800ED80B66
K-Meleon-R-r15-x64-portable.zip 503C49275B6171DB9F77218B3C731B6191F2ECC007BA1AC7471C2685678D4AAD
```
