# K-Meleon-R r17 (8.8.1) - SHODAN pane window-mode fixes

**Patch release.** r17 fixes the SHODAN side-pane window modes introduced in r16
and makes them coherent and exception-safe. No other behavior changes.

## Downloads

- [Windows x64 EXE installer](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.1-r17/K-Meleon-R-r17-x64-setup.exe)
- [Windows x64 portable ZIP](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.1-r17/K-Meleon-R-r17-x64-portable.zip)
- [Always get the latest release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest)

## Fixes

- **Pane window modes no longer throw.** The float/dock/close paths disposed the
  floating window (and with it the live SHODAN WebView2 and its session) before
  reparenting the pane, which raised an `ObjectDisposedException` and left the
  pane broken - which also made the avatar/face appear to "fail". The live pane
  is now always reparented out of the float window first, through a single safe
  helper, and is never disposed until the browser closes.
- **Full mode set, coherent together:** minimize (collapse to a labelled rail
  when docked, or minimize the window when floating), expand width, fullscreen
  (borderless-maximized float, Esc to exit), float out into a real window and
  dock back, and close - each reversible and keeping the live SHODAN session.
- **Closing the float window via the OS button** (or Alt+F4) now docks the pane
  back instead of destroying the session.

## Validation

Verified at runtime on .NET 10 by driving every mode (minimize, expand,
fullscreen, float, dock-back, close, and OS-close of the float) through UI
Automation: the app stays alive with no exceptions, and the SHODAN avatar keeps
rendering (`canvas-portrait`, live viseme lip sync) in both docked and floating
states. The 59 native + 10 start-page tests still pass.

## SHA-256

```
K-Meleon-R-r17-x64-setup.exe    (published with the GitHub release)
K-Meleon-R-r17-x64-portable.zip (published with the GitHub release)
```
