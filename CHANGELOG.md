# Changelog

All notable changes to **ModernLrc** are documented in this file. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.1.0]: https://github.com/Anthonyy232/ModernLrcParser/releases/tag/v1.1.0
[1.0.0]: https://github.com/Anthonyy232/ModernLrcParser/releases/tag/v1.0.0
