# K-Meleon-R Start Page for Pale Moon

The K-Meleon-R start page as a Pale Moon extension, so it runs on a current
Pale Moon engine instead of the old Goanna build. MSN and Gemini render fully
there.

It adds:

- An Opera GX-style speed dial: MSN, Google, YouTube, Bandcamp and Spotify by
  default, with add, edit and remove, your own thumbnails and site logos.
- Search over Bing, Google or Copilot (button cycles them).
- **Ask Copilot** (Bing Copilot Search) and **Ask Gemini** (copies your text,
  then opens Gemini).
- A Dark / Light / System theme toggle.

It is set as the home page and new-tab page the first time it runs. Change them
in Settings and the extension leaves your choice alone. The page is also at
`about:kmr`.

## Install

1. Get Pale Moon from <https://www.palemoon.org/>.
2. Download `kmeleon-r-startpage.xpi` from the releases page.
3. Drag it into a Pale Moon window, or open it from `Tools > Add-ons`.
4. Restart Pale Moon.

Pale Moon's licence does not allow redistributing its binaries with add-ons
added, so this project ships only the extension.

## Build

`python build.py` writes `kmeleon-r-startpage.xpi` next to this file.

## Credits

River Lyle Reuveni (TheImortalimp), AI & .co, with GitHub Copilot.
