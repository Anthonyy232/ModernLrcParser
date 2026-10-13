# Changelog

All notable changes to **ModernLrc** are documented in this file. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Removed (breaking)

- `LrcParseOptions.ReadBufferSize` and `LrcWriteOptions.InitialBufferSize`. Inputs
  and outputs are small enough that the BCL defaults are fine.
- `LrcWriter.EstimateSize` and `LrcWriter.EstimateByteSize`. They were heuristics that
  duplicated the renderer; render with `Write` or an `IBufferWriter<T>` overload instead.
- The public enums are no longer `byte`-backed.

### Changed

- `LrcParseOptions.FallbackEncoding` now defaults to `Encoding.Latin1`, which decodes
  every byte losslessly. The previous default (`Encoding.UTF8`) re-decoded bytes that
  had just failed UTF-8 validation, turning them into U+FFFD.
- Typed metadata properties are now authoritative when writing. Raw tags of a known
  key are written verbatim (keeping duplicates and order) only while they still agree
  with the typed property, so `metadata with { Title = "New" }` is no longer ignored.
- `LrcDocumentBuilder.WithRawTag` with a known key (`ti`, `length`, …) also sets the
  matching typed property, as the parser does.
- All writer sinks share one char renderer; byte sinks encode its output. The
  separate UTF-8 byte renderer is gone.

### Fixed

- `LrcTimestamp.ToString` / `TryFormat` now throw `FormatException` for unsupported
  format codes instead of silently formatting as `"G"`.
- `LrcWriteOptions.EmitByteOrderMark = true` now writes a BOM with the default encoding.
  The default was a BOM-less `UTF8Encoding`, so the option silently did nothing.
- The build no longer fails on restore: Microsoft.SourceLink.GitHub is bumped past
  GHSA-23fw-v26w-5fgq, and NuGet audit warnings no longer fail the build.

## [1.2.0] — 2026-05-02

### Changed

- `LrcTimestamp.TotalMilliseconds` now returns `long` instead of `int`, matching
  the timestamp range accepted by parsing and factory APIs without overflowing
  for values beyond `int.MaxValue` milliseconds.
- `LrcDocumentBuilder` typed metadata setters now remove matching raw typed tags
  from seeded documents so explicit edits win on write.

### Fixed

- Tolerant Enhanced LRC recovery now preserves literal trailing text after an
  invalid enhanced timestamp appears later in an otherwise valid enhanced line.
- Parsed duplicate/conflicting typed metadata tags now round-trip through the
  writer instead of collapsing to only the last-wins typed accessor.
- The release workflow now runs the Native AOT publish/smoke check before
  packing and publishing.

## [1.1.1] — 2026-04-30

### Added

- Added a `pre-commit` configuration with common hygiene hooks and `dotnet format`.

### Fixed

- Stream parsing now honors the current `MemoryStream.Position` on the buffer
  fast path and consumes the stream to its end, matching other stream inputs.
- Strict parsing now still throws when an Error diagnostic is suppressed by
  `MaxDiagnostics`, including `MaxDiagnostics = 0`.
- `IBufferWriter<byte>` and fixed `Span<byte>` write paths now honor non-UTF-8
  `LrcWriteOptions.Encoding` instead of writing a UTF-8 body after an arbitrary
  preamble.
- `LrcTimestamp.FromMilliseconds` now rejects values that would overflow the
  non-negative tick invariant.

## [1.1.0] — 2026-04-28

### Changed (breaking)

- **`LrcLine.Timestamps` (collection) is replaced by `LrcLine.Timestamp` (scalar).**
  Multi-timestamp groups `[t1][t2]text` are fanned out at parse into N
  `LrcLine` instances sharing the same `Text` reference, and re-collapsed at
  write when `LrcWriteOptions.CollapseIdenticalLines` is enabled.
- **`LrcWriteOptions.CollapseIdenticalLines` default flipped from `false` to
  `true`** — the writer is now the round-trip mechanism for inputs originally
  written as multi-timestamp groups. Set to `false` to emit one timestamp per line.
- **Builder API:** `LrcDocumentBuilder.AddLine(ReadOnlySpan<LrcTimestamp>, ...)`
  replaced by `AddLineGroup(ReadOnlySpan<LrcTimestamp>, ...)`; the enhanced-line
  span overloads `AddEnhancedLine(ReadOnlySpan<LrcTimestamp>, ...)` likewise
  replaced by `AddEnhancedLineGroup(ReadOnlySpan<LrcTimestamp>, ...)`. All
  single-timestamp `AddLine` / `AddEnhancedLine` overloads are unchanged.

## [1.0.0] — 2026-04-27

Initial public release. Modern, AOT-compatible LRC (lyrics) parser/writer for
.NET 10 — full LRC support (basic, multi-timestamp, Enhanced word timing,
Walaoke voice markers), tolerant-by-default with a stable diagnostic catalogue,
sync + async overloads for every input shape, and zero-allocation writer paths.

[1.2.0]: https://github.com/Anthonyy232/ModernLrcParser/releases/tag/v1.2.0
[1.1.1]: https://github.com/Anthonyy232/ModernLrcParser/releases/tag/v1.1.1
[1.1.0]: https://github.com/Anthonyy232/ModernLrcParser/releases/tag/v1.1.0
[1.0.0]: https://github.com/Anthonyy232/ModernLrcParser/releases/tag/v1.0.0
