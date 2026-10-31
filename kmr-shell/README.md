# K-Meleon-R r11

A small, fast, standalone K-Meleon-R browser: Opera GX-style speed dial, tabs,
Dark theme plus three light themes (Light, Sand, Sky) and System, and Bing, Google, Copilot and Gemini hooks. It
renders pages with the Microsoft Edge WebView2 runtime (already part of
Windows 10/11), so MSN, Google, YouTube, Spotify and Gemini all work. It does
not need Pale Moon.

- Start page: MSN, Google, YouTube, Bandcamp, Spotify, then Opera and Opera GX quick dials; add, edit, remove, your own thumbnails. Existing speed dials get the two Opera tiles once; removing them sticks.
- Themes: the Theme button cycles Dark, Light, Sand, Sky and System. The toolbar, tabs and the start page change together; light themes have their own palettes, hover colours and tile styling.
- Address bar: type a URL or a search. The Search button cycles Bing / Google / Copilot.
- **Ask Copilot** opens Bing Copilot Search with your text.
- **Ask Gemini** copies your text to the clipboard and opens Gemini; paste it there.
- Shortcuts: Ctrl+T new tab, Ctrl+W close, Ctrl+Tab next tab, Ctrl+L address bar, F5 reload, Alt+Home start page.

Settings and profile live in `%LOCALAPPDATA%\K-Meleon-R`.

This is a new shell. It reuses the K-Meleon start page and design, not K-Meleon's
Gecko C++ code. See NOTICE-K-Meleon-R.md for credits.

Credits: River Lyle Reuveni (TheImortalimp), AI & .co, with GitHub Copilot.

## Changelog

### r11 (8.3.0)
- Fixed web apps that detect a native WebView2 host: the AI WARP web edition could not play YouTube in K-Meleon-R. The host bridge is now hidden from ordinary web pages; only the start page keeps it for theme syncing. Verified: the YouTube player reports playing.
- Tracking prevention is off so CDN scripts and embedded players can use storage.
- Optional KMR_DEBUG_PORT environment variable enables remote debugging.

### r10 (8.2.0)
- Better WebView2 handling: autoplay allowed for embedded players (YouTube, Spotify), true fullscreen for video (tabs and toolbar hide), automatic reload after a renderer crash or hang, autofill, password save, zoom, swipe navigation and F12 DevTools.
- NOTICE now carries the licence wording published at imortalimp.nl.
### r9 (8.1.0)
- New Opera and Opera GX quick-dial tiles after the five curated tiles (added once to existing dials).
- Light mode reworked into three themes: Light, Sand and Sky, plus System. The Theme button cycles them.
- Toolbar, tabs and start page now share one theme (synced both ways); the WebView2 background and colour scheme follow it.
- Original SVG icons for the Opera tiles (not official logos).

### r8
Final release of the previous line.

## Install note
The installer is unsigned, so Windows SmartScreen may warn. Choose More info, then Run anyway. SHA-256 hashes are in each release's notes.

## System cursor only
Web pages cannot change the mouse pointer. On launch and on every change, any pointer not empty or under %WINDIR%\Cursors is reset (see CursorGuard.cs). The installer runs elevated and applies cursor_lockdown.ps1 for all local profiles.
