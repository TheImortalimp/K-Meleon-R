# K-Meleon-R r18 (8.8.2) - SHODAN face/avatar bundling fix

**Patch release.** r18 fixes the SHODAN avatar (face) not rendering in the
shipped build. No other behavior changes.

## Downloads

- [Windows x64 EXE installer](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.2-r18/K-Meleon-R-r18-x64-setup.exe)
- [Windows x64 portable ZIP](https://github.com/TheImortalimp/K-Meleon-R/releases/download/v8.8.2-r18/K-Meleon-R-r18-x64-portable.zip)
- [Always get the latest release](https://github.com/TheImortalimp/K-Meleon-R/releases/latest)

## Fix

- **SHODAN face now renders in the release build.** The single-file publish
  (`PublishSingleFile=true`) silently dropped exactly one bundled SHODAN asset -
  `shodan/avatar.png`, the face image - from the shipped `shodan` folder, so the
  `kmr.shodan` virtual host returned 404 for it and the canvas portrait never
  became ready. (It built fine with plain `dotnet build`, which is why earlier
  smoke tests passed, but the release uses single-file publish.) The SHODAN pane
  assets are now marked `ExcludeFromSingleFile`, so every file - including the
  avatar - is shipped as a real file next to the exe. The release workflow also
  now fails fast if `avatar.png` is missing or the bundled SHODAN file count is
  short.

## Validation

Verified against the actual single-file self-contained publish layout: all 52
bundled SHODAN files are present, and the SHODAN pane's avatar renders at runtime
(`canvas-portrait`, live viseme lip sync, `avatar.png` loaded at 1024px). The
59 native + 10 start-page tests still pass.

## SHA-256

```
K-Meleon-R-r18-x64-setup.exe    (published with the GitHub release)
K-Meleon-R-r18-x64-portable.zip (published with the GitHub release)
```
