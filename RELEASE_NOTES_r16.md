# K-Meleon-R r16 (8.8.0) - .NET 10, SHODAN side pane, hourly hardening and hardened pop-ups

**Current/latest stable release.**
r16 supersedes r15 and earlier builds. This feature release moves the shell to
the current .NET LTS, adds the bundled SHODAN voice-AI side pane, runs the
GEM-OS lockdown engine silently every hour, and replaces silent pop-ups with a
hardened, verified pop-up handler.

## Downloads

- [Windows x64 EXE installer](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.0-r16/K-Meleon-R-r16-x64-setup.exe)
- [Windows x64 portable ZIP](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.0-r16/K-Meleon-R-r16-x64-portable.zip)
- [Always get the latest release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest)

Both packages include updated documentation and `RELEASE_NOTICE.md`.

## What's new

- **.NET 10 (LTS).** The shell and its search tests now target `net10.0`
  (.NET 8 reached end of support on 10 November 2026). The release build,
  59 native search tests, 10 start-page tests and the self-contained
  single-file publish all pass on .NET 10.
- **SHODAN side pane.** A new **SHODAN** toolbar button (Ctrl+Shift+S) opens a
  right-docked, resizable pane hosting the bundled SHODAN voice AI. The pane can
  be **expanded** to full width to show every panel and **popped out** into its
  own floating window and docked back again. It is bundled offline and served
  from the local `kmr.shodan` virtual host, so it works with no internet.
- **Merged flint:40b brain.** SHODAN ships merged: the `flint:40b` local LLM is
  her default conversational *and* monologue voice, with CORTEX kept underneath
  as her soul, memory and instant fallback. The bundled `install-flint.ps1`
  provisions the model (`ollama pull flint:40b`) and allows the pane's origin;
  everything stays on loopback.
- **Hourly silent hardening.** An elevated install registers a hidden
  "K-Meleon-R Hourly Hardening" scheduled task that runs the bundled GEM-OS
  `Hardening_Lockdown_Standalone.bat` silently every hour with the highest
  privileges (no UAC prompt). The task is removed on uninstall.
- **Hardened pop-up handler.** Pop-ups now load their content, but never
  silently: a target that Google Safe Browsing verifies as malicious is refused
  outright, and anything else opens in its own guarded window carrying an
  "allowed pop-up" notice and a one-click "Open as tab" promotion. Pop-up
  windows inherit the full guard stack (Cursor/Defender/SafeBrowsing/Capture)
  and are screen-capture shielded like every tab.

## Requirements and boundaries

Windows 10/11 x64 and the Microsoft Edge WebView2 Runtime are required. The
merged neural link needs a local OpenAI-compatible server (Ollama by default)
with `flint:40b` pulled; without it SHODAN falls straight back to CORTEX and
stays fully offline. Safe Browsing pop-up verification is active only when a
Google Safe Browsing API key is configured; otherwise SmartScreen still covers
reputation and non-malicious pop-ups are allowed.

The installer is unsigned. Verify its checksum and source before running it.
An elevated install applies Windows policy hardening and the hourly hardening
task; the policy hardening is not undone by uninstall, but the scheduled task
is removed. Portable packages never change machine policies automatically.

## Validation

Native search regression tests (59) and start-page tests (10) pass on .NET 10.
The Windows x64 self-contained publish, the SHODAN pane build/bundle, and the
NSIS installer compilation succeeded. SHODAN's own suite (58 tests) passes with
the flint merge. These checks are not a guarantee that all websites or
machine-policy behavior work.

## SHA-256

```
K-Meleon-R-r16-x64-setup.exe    (published with the GitHub release)
K-Meleon-R-r16-x64-portable.zip (published with the GitHub release)
```
