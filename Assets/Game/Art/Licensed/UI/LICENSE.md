# UI pack sprite — license record

- Source: https://kenney.nl/assets/ui-pack
- Author: Kenney (https://kenney.nl)
- License: CC0 1.0 Universal (https://creativecommons.org/publicdomain/zero/1.0/)
- Original paths in zip: `kenney_ui-pack.zip` → `PNG/Yellow/Default/star.png`,
  `Font/Kenney Future.ttf`
- Files: `star-yellow.png` (renamed, pixels unmodified), `Fonts/KenneyFuture.ttf` (renamed,
  unmodified; SHA-256 `7a55b07f5968fac872648a7c5e959bd2b93e06f63153b585d56e4d5298ddff61`). The
  font sets the HUD numbers, menu titles, buttons and prompts; outlines are runtime UI effects.
- Attribution: not required by CC0; credited here anyway per Kenney's request ("UI Pack" by
  Kenney, www.kenney.nl).

## Display font: Lilita One

- Source: https://github.com/google/fonts/tree/main/ofl/lilitaone (`LilitaOne-Regular.ttf`, `OFL.txt`)
- Author: Juan Montoreano (Copyright (c) 2011, Reserved Font Name "Lilita")
- License: SIL Open Font License 1.1 (full text in `Fonts/LilitaOne-OFL.txt`). Embedding in an app is permitted;
  the font is unmodified and not sold on its own.
- Files: `Fonts/LilitaOne-Regular.ttf` (unmodified; SHA-256 `f5b641c45c69d772ee4eda687bc9fda411d5cad6b0b45371491da4580cbc8d59`).
- Why: it replaces Kenney Future for the HUD numbers, titles, buttons and prompt. Kenney Future's uppercase X
  is drawn as an H with two small notches (checked by rendering both glyphs), so EXIT read as EHIT at any size.
  Lilita One is a bold rounded game face with an unmistakable X.
- `Fonts/KenneyFuture.ttf` is no longer referenced by the scene builders and is kept only as a record.
