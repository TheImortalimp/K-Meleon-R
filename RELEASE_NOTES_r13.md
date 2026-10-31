# K-Meleon-R r13 (8.5.0) - GEM OS hardening

- **Theme/pointer package blocking:** the embedded WebView2 now cancels downloads and navigations of .theme, .themepack, .deskthemepack, .cur and .ani files (trailing dot/space tricks included) and refuses ms-settings: navigation. External links open only for http/https.
- **Runtime policy:** the guard also sets NoChangingSoundScheme alongside NoChangingMousePointers.
- **Installer:** ships and runs harden_against_gem_os.ps1 -Apply (see the GEM-OS repo). Quarantine of user files stays opt-in.
- **Limits:** an administrator can undo the policies; installers are unsigned so SmartScreen warns (code signing is the real fix); a clean scan is not proof of safety. -Apply was not run end to end on a real machine.
Installer SHA-256 (K-Meleon-R-r13-x64-setup.exe): 5CD14BA6F6D297B9E2D492DF69D04477699A8A46F581FE5AD188DA8F5EFE9EFB

