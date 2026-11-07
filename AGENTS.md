# Working on ModernLrcParser

Guidance for anyone changing this repo, human or AI. These are defaults, not laws: follow them unless you have a good reason not to, and say what the reason was.

ModernLrcParser is the `ModernLrc` NuGet package: an AOT- and trim-compatible .NET 10 library for parsing, editing and writing `.lrc` lyrics. See the [README](README.md) and `docs/`.

## How we write code

- **Simple beats clever.** Write the most direct code that solves today's problem. No speculative abstractions, options or extension points for needs that don't exist yet.
- **Stay in scope.** Build what was asked; suggest extras instead of adding them.
- **Don't hand-roll what already exists** in the BCL or this repo. Search before you write a helper.
- **Not overly defensive.** Validate at public entry points and trust internal callers. In tolerant mode, malformed input yields a diagnostic, never an exception.
- **One source of truth.** Don't keep two copies of the same state that can disagree.
- **Delete dead code** you come across or create.
- **Match the surrounding code.** Consistency across files matters more than personal preference.
- **Fix it everywhere.** After fixing a bug or a convention slip, look for the same pattern elsewhere.

## Public API and docs

- **The public API is a semver contract.** A removed or changed public member means a major version and a `!` commit (`refactor!:`).
- **Every user-visible change gets a CHANGELOG entry** under `[Unreleased]`.
- **Keep docs in step with behaviour.** Update the XML docs, the matching `docs/articles` page and the README in the same change.

## Performance

- **Parsing and writing are hot paths.** Benchmark changes to them against the last release (`bench/`) and report time and allocations.
- **Prove a rewrite doesn't change behaviour** by fuzzing its output against the last release.
- The README comparison tables come from the `bench/comparison` branch; sync its `src/` with `main` before re-running it.

## Comments

- Explain **why**, not **what**: a constraint, a format quirk, a non-obvious decision.
- Don't restate names or code, narrate the next line, or label sections.
- Public members need XML docs; make them add what the signature doesn't. Internal members get them only when they add something.

## Tests

- Test **behaviour and the edges that can break**: malformed input, recovery, round trips, encodings, overflow, cancellation. Use CsCheck properties for invariants.
- Skip trivial, duplicate or obvious tests: constructors, `init` properties, record equality, null-guard throws, tests that can't fail. Prefer one theory over near-identical tests.
- Run tests through the test project as CI does: `dotnet test` on the solution can pass without running anything.

## Before you call something done

- It builds with **zero warnings**, all tests and the Native AOT smoke test pass, and `dotnet format` is clean.
- You have **reviewed your own diff against this file**: over-engineering, over-abstraction, defensive code, hand-rolled or duplicated code, low-signal tests, restating comments.
- Behaviour changes are deliberate and called out, not side effects of a refactor.

## Git and releases

- **Commit messages:** `type: summary` in lowercase imperative, e.g. `perf: restore parse speed, pool writer staging buffer`. Types: `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `chore`, `ci`; add `!` for breaking changes. Use a short body (bullets) only to explain why.
- **One logical change per commit.**
- **No AI attribution** in commits, PRs, branch names or release notes.
- Work on a branch off `main` and merge through a PR after CI passes on all three OSes; keep `main` linear. Never force-push `main`.
- **Releasing:** pushing a `vX.Y.Z` tag publishes to NuGet; also create a GitHub Release from the CHANGELOG section.

## For AI agents

- Ask before anything hard to undo or visible to others: pushing, PRs, merging, tags, releases, deleting branches, rewriting history. Approval for one step isn't approval for the next.
- Report results honestly: what was verified and how, and what wasn't.
- Keep replies short; use bullets when they help. Prefer one clear recommendation over a list of options.
