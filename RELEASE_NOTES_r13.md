# K-Meleon-R r13 (8.5.0) - GEM OS hardening

- **Theme/pointer package blocking:** the embedded WebView2 now cancels downloads and navigations of .theme, .themepack, .deskthemepack, .cur and .ani files (trailing dot/space tricks included) and refuses ms-settings: navigation. External links open only for http/https.
- **Runtime policy:** the guard also sets NoChangingSoundScheme alongside NoChangingMousePointers.
- **Installer:** ships and runs harden_against_gem_os.ps1 -Apply (see the GEM-OS repo). Quarantine of user files stays opt-in.
- **Limits:** an administrator can undo the policies; installers are unsigned so SmartScreen warns (code signing is the real fix); a clean scan is not proof of safety. -Apply was not run end to end on a real machine.
Installer SHA-256 (K-Meleon-R-r13-x64-setup.exe): 5CD14BA6F6D297B9E2D492DF69D04477699A8A46F581FE5AD188DA8F5EFE9EFB

## Version and download

- App version: **8.5.0**; release label: **r13**; tag: **v8.5.0-r13**.
- [Download K-Meleon-R-r13-x64-setup.exe](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.5.0-r13/K-Meleon-R-r13-x64-setup.exe).
- This release currently supplies an installer only, not an r13 portable ZIP.
- Requires Windows 10/11 x64 and Microsoft Edge WebView2.
- Installation defaults to `%LOCALAPPDATA%\Programs\K-Meleon-R` and requires
  UAC elevation for Windows cursor/theme/sound policy changes.

## Retained changes

- **r12 (8.4.0):** system-pointer-only page/runtime guards, pointer-path validation,
  process hardening and elevated cursor lockdown with registry backups.
- **r11 (8.3.0):** native host bridge hidden from ordinary web pages, retained for
  local start-page theme syncing; embedded-player compatibility and optional
  `KMR_DEBUG_PORT`.
- **r10/r9:** WebView2 playback/fullscreen/recovery handling, editable speed
  dials and five synchronized themes.

## Documentation refresh

Root and shell READMEs now describe r13/8.5.0 consistently. New About and
Description pages cover features, version history, installation policy effects
and compatibility/privacy boundaries. GitHub About is updated from r12 to r13.

This refresh is documentation-only: no installer rebuild or binary replacement,
no new release number, and no claim of fresh end-to-end runtime/policy testing.
The existing installer checksum above is unchanged.

Tracking prevention and reputation checking are disabled in the current host;
clipboard-read requests are automatically allowed. This favors compatibility
and is not a strict privacy configuration. Uninstall does not automatically
restore Windows policy changes. See [About](ABOUT.md) and
[shell documentation](kmr-shell/README.md) for details.
