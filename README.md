# ModernLrc media

Generated images for [ModernLrc](https://github.com/Anthonyy232/ModernLrcParser), kept off `main` so it stays light. Everything here is rebuilt from files on this branch and a checkout of `main`; nothing is fetched at build time.

| Output | Made by |
| --- | --- |
| `main`: `.github/readme/hero-*.png`, `feature-*-*.png` (2048 px wide, light and dark) | `readme/build.cjs` |
| `main`: `assets/icon.png` (128x128 NuGet icon, from `assets/logo.svg`) | `readme/build.cjs` |
| this branch: `social-preview.png` (1280x640, the hero at 640 px tall) | `readme/build.cjs` |
| this branch: `releases/vX.Y.Z.png` (1280x640 release banners) | `releases/banner.cjs` |

The README panels show real code and what it printed. `readme/capture/` is a console project holding the exact snippets, one `#region` each, built against the library in the `main` checkout. Running it writes each snippet (`readme/inputs/<name>.cs.txt`) and its output (`<name>.out.txt`) as text, and `build.cjs` draws the panels from those files. Headlines and copy come from `site/index.html` on `main`, the logo from `assets/logo.svg`.

Needs Node, the .NET 10 SDK, and Windows (the panels use the system's Segoe UI and Cascadia Code; Instrument Serif is in `fonts/`).

## Setup

Check this branch out beside the repository, not inside it (the capture project would pick up the repository's MSBuild files):

```sh
git worktree add ../ModernLrc-media media
cd ../ModernLrc-media
npm ci
npx playwright install chromium
```

## README panels, package icon and social preview

```sh
dotnet run --project readme/capture -p:Checkout=<path to main checkout>
node readme/build.cjs <path to main checkout>
```

Re-run the capture whenever the library or a snippet changes: the panels then show what the current library prints. To change a snippet, edit its region in `readme/capture/Program.cs`, and keep the README, the site and the snippet in step. Look at every image, then commit `readme/inputs/` and `social-preview.png` here and the images on `main`. GitHub has no API for the social preview: upload `social-preview.png` in the repository's Settings, under Social preview.

## Release banners

```sh
node releases/banner.cjs <path to main checkout> X.Y.Z "One short highlight"
```

That records the highlight in `releases/banners.json` and writes `releases/vX.Y.Z.png`. Without a version it redraws every banner. Commit, push, and open the release notes with the raw URL:

`https://raw.githubusercontent.com/Anthonyy232/ModernLrcParser/media/releases/vX.Y.Z.png`
