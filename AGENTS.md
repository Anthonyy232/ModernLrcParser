# Working on ModernLrc

The `ModernLrc` NuGet package: an AOT- and trim-compatible .NET 10 library that parses, edits and writes `.lrc` lyrics. The repo is `ModernLrcParser`; the product name everywhere else is ModernLrc.

## Code

- Simple and direct. No speculative abstractions, options or extension points.
- Stay in scope; suggest extras instead of adding them.
- Use what the platform, framework, dependencies and this repo already provide before writing a helper.
- Validate only at real boundaries (user input, storage, network, other processes). No checks, fallbacks or try/catch for cases that can't happen.
- One source of truth for each value or rule.
- Delete dead code you find or create.
- Match the surrounding code.
- Fix root causes, and fix the same pattern everywhere it occurs.

## Comments

- Only where the reason isn't clear from the code: a quirk, a race, a non-obvious decision.
- Short fragments, not sentences. Never restate code, narrate, or label sections.
- Delete comments that no longer match.
- Public members need XML docs (they become the API reference); say what the signature doesn't.

## Tests

- Behaviour and the edges that can break. No trivial, duplicate, obvious or low-signal tests.
- A regression test fails without its fix.
- Deterministic: fake time and observable events, no sleeps.
- A failing test is a bug until shown otherwise.
- Run the test project, not the solution: `dotnet test` on the solution can pass without running anything.
- Invariants (round trips, recovery) go in CsCheck properties.

## Public API

- The public API is a semver contract. Removing or changing a public member needs a major version and a `!` commit (`refactor!:`).
- In tolerant mode malformed input yields a diagnostic, never an exception. Diagnostic codes are stable: don't renumber or reuse them.
- Every user-visible change gets a `CHANGELOG.md` entry under `[Unreleased]`, and updates the XML docs, the matching `docs/articles` page and the README in the same change.
- Parsing and writing are hot paths. Benchmark changes to them against the last release (`bench/`) and report time and allocations; prove a rewrite keeps behaviour by fuzzing its output against the last release.
- The README comparison tables come from the `bench/comparison` branch; sync its `src/` with `main` before re-running it.

## Docs and site

- `site/` is the hand-written landing page; `docs/` is DocFX (articles, API reference from the XML docs, the theme in `docs/template/`). `docs.yml` builds both into one GitHub Pages site on every push to `main`. Published URLs under `articles/` and `api/` must keep working.
- nuget.org renders no HTML, so the package ships `src/ModernLrc/README.md`, not the root README. Keep the two in step.
- Code shown in README panels, the site and the articles must have been compiled and run; show the output it really printed.

## Done means

- Diff reviewed against this file.
- These pass with zero warnings:
  ```sh
  dotnet build ModernLrcParser.sln -c Release
  dotnet test tests/ModernLrc.Tests/ModernLrc.Tests.csproj -c Release --no-build
  dotnet format ModernLrcParser.sln --verify-no-changes
  ```
  For changes to `src/`, also publish and run `tests/ModernLrc.Tests.Aot` as `pr.yml` does.
- Site changes seen in a browser, light and dark. Docs and metadata changes need no end-to-end run.
- README updated for anything users will notice; behaviour changes called out.

## Docs

- README is for users: what it does, how to get it, how to use it. No internals, no agent instructions.
- This file is for agents: only what can't be found quickly in the code.
- No meta docs in the repo (notes, audits, plans, reports).

## Git

- `type: summary` or `type(scope): summary`: lowercase, imperative, no full stop, about 70 characters at most. Types: `feat` `fix` `refactor` `perf` `test` `docs` `style` `build` `ci` `chore`.
- Body only to say why. `Fixes #123` closes an issue.
- One logical change per commit. Squash fix-ups before merging.
- Branch off `main`. A single commit fast-forwards; several merge with `merge: <summary>`.
- No AI attribution in commits, branches, PRs or releases.
- One maintainer: amending and force-pushing to tidy history is fine. Keep contributors as authors. Delete stale branches.
- Published packages record their commit SHA (SourceLink). Never rewrite a commit at or before the latest release tag, and never move a release tag.

## Pull requests

- Title in commit format. Body: what changed, why, how it was checked. Short.

## Releases

- Bump commit `chore: release vX.Y.Z` (`<Version>` in `src/ModernLrc/ModernLrc.csproj`, the `CHANGELOG.md` section); tag `vX.Y.Z`.
- Pushing a `v*` tag makes `release.yml` publish to nuget.org. A published version can't be replaced, only unlisted, so tag last and only the commit that CI passed on. nuget.org trusts the workflow by its file name: don't rename it.
- Title `ModernLrc X.Y.Z`.
- Notes: banner image first, then `### New`, `### Improved`, `### Fixed` (only those that apply). One line per change, written for users; no internals, dependency lists or test counts. End with the install command, the NuGet link and the full changelog link.
- Banner: `node banner.cjs <version> "<highlight>"` on the orphan `media` branch writes `releases/vX.Y.Z.png`; reference its raw URL.

## Agents

- Pushing, amending and force-pushing are allowed. Ask before publishing a release or submitting to a store; pushing a `v*` tag is publishing.
- Leave changes you didn't make alone. Clean up scratch files, processes and emulators.
- Say what was checked and how, and what wasn't.
- Short replies; one recommendation, not a list of options.
