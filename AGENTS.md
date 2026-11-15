# Working on ModernLrc

A .NET library that parses, edits and writes `.lrc` lyrics files, kept Native AOT and trim compatible. It reaches users as the `ModernLrc` package on nuget.org, with a landing page and docs on GitHub Pages. The repo is `ModernLrcParser`; the product name everywhere else is ModernLrc.

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

## Tests

- Behaviour and the edges that can break. No trivial, duplicate, obvious or low-signal tests.
- A regression test fails without its fix.
- Deterministic: fake time and observable events, no sleeps.
- A failing test is a bug until shown otherwise.
- Invariants (round trips, recovery from bad input) are property tests.

## Library

- The public API, its documented behaviour and the diagnostic codes are a contract with package consumers. Removing or changing any of them needs a major version and a `!` commit (`refactor!:`); never renumber or reuse a code.
- Every user-visible change gets a changelog entry under Unreleased and updates the API docs, the matching article and both READMEs in the same change. nuget.org renders no HTML, so the package ships its own README.
- Parsing and writing are hot paths: benchmark a change to them against the last release and report time and allocations.
- Code shown in the README, the site and the articles must have been compiled and run; show the output it really printed.
- Published docs URLs must keep working: leave a redirect when a page moves or goes.

## Done means

- Diff reviewed against this file.
- `dotnet build -c Release`, `dotnet test -c Release` and `dotnet format --verify-no-changes` pass with zero warnings; library changes also pass the AOT smoke test that CI runs.
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
- Published packages record their commit SHA. Never rewrite a commit at or before the latest release tag, and never move a release tag.

## Pull requests

- Title in commit format. Body: what changed, why, how it was checked. Short.

## Releases

- Bump commit `chore: release vX.Y.Z` (package version and changelog section); tag `vX.Y.Z`.
- Title `ModernLrc X.Y.Z`.
- Notes: banner image first, then `### New`, `### Improved`, `### Fixed` (only those that apply). One line per change, written for users; no internals, dependency lists or test counts. End with the install command, the NuGet link and the full changelog link.
- Banner: `node banner.cjs <version> "<highlight>"` on the orphan `media` branch writes `releases/vX.Y.Z.png`; reference its raw URL.
- Draft the GitHub release first. Pushing the `v*` tag is the only thing that publishes: CI pushes the package to nuget.org, then publishes the draft. A published version can't be replaced, only unlisted, so tag last and only a commit CI passed on. nuget.org trusts the release workflow by its file name: don't rename it.

## Agents

- Pushing, amending and force-pushing are allowed. Ask before publishing a release or submitting to a store; pushing a `v*` tag is publishing.
- Leave changes you didn't make alone. Clean up scratch files, processes and emulators.
- Say what was checked and how, and what wasn't.
- Short replies; one recommendation, not a list of options.
