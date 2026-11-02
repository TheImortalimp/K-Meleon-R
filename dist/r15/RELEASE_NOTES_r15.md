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

## SHA-256
```
K-Meleon-R-r15-x64-setup.exe    CCA7FFB5F8337EE22E17185B1C3E719D0C0E703B2A16670C8A49DE800ED80B66
K-Meleon-R-r15-x64-portable.zip 503C49275B6171DB9F77218B3C731B6191F2ECC007BA1AC7471C2685678D4AAD
```
