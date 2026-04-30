# ModernLrc

Modern, performance-first LRC (lyrics) parser and writer for .NET 10. AOT-compatible, trim-safe, zero non-BCL runtime dependencies.

📖 **Full documentation, API reference, and topical guides:** <https://anthonyy232.github.io/ModernLrcParser>
(also browseable in the [`docs/`](docs/) folder).

## Features

- **Full LRC support** — simple `[mm:ss.xx]text`, multi-timestamp `[t1][t2]…`, Enhanced LRC word timing `<…>`, Walaoke voice markers (`M:` / `F:` / `D:`) with state propagation.
- **Tolerant by default** with rich diagnostics; opt-in `LrcStrictness.Strict` for fail-fast behaviour.
- **Span-first hand-rolled scanner** — no regex, no backtracking, zero per-token transient allocations.
- **Every input shape** — `string`, `ReadOnlySpan<char>`, `ReadOnlyMemory<char>`, `TextReader`, `ReadOnlySpan<byte>`, `byte[]`, `Stream`, file paths; sync and async.
- **Zero-allocation writer** via `IBufferWriter<char>` and UTF-8 byte sinks; the `Write → string`, `TextWriter`, `TryWrite`, and `Stream` sinks pre-size their staging buffer to the document's estimated size so large renders don't accumulate copy-on-grow waste; atomic file write via temp + rename.
- **Encoding pipeline** — BOM detection (UTF-8, UTF-16 LE/BE) → caller override → UTF-8 validation → caller-supplied fallback.
- **Diagnostics catalogue** — every recoverable concern surfaces as a stable diagnostic code (`LRC0001`–`LRC0099`) with line/column location.

## Install

```sh
dotnet add package ModernLrc
```

## Requirements

- .NET 10 runtime (consumers).
- .NET 10 SDK (pinned via `global.json`) only required to build from source.

## Quick start

```csharp
using ModernLrc;
using ModernLrc.Model;

// Parse
var result = LrcParser.Parse("[ti:Demo]\n[00:01.00]hello\n");
Console.WriteLine(result.Document.Metadata.Title); // Demo

// Author + write
var doc = new LrcDocumentBuilder()
    .WithTitle("My Song")
    .AddLine("00:12.00", "first line")
    .AddLine("00:14.20", "second line", LrcVoice.Female)
    .Build();
string lrc = LrcWriter.Write(doc);
```

## Design notes

### Offset model

Documents store the `[offset:N]` tag verbatim in `Metadata.Offset`. Line timestamps are NOT mutated at parse time. Apply the offset on demand:

```csharp
var doc = LrcParser.Parse(text).Document;
TimeSpan effective = doc.GetEffectiveTime(line.Timestamp);
LrcLine? current = doc.FindLineAt(currentPlayhead);  // already factors offset in
```

This preserves round-trip fidelity — re-writing the document emits the original `[offset:N]` tag rather than collapsing it into mutated timestamps.

### Encoding model

Byte/stream input uses this priority:

1. BOM detection (UTF-8 `EF BB BF`, UTF-16 LE `FF FE`, UTF-16 BE `FE FF`).
2. Caller-supplied `LrcParseOptions.Encoding` if set.
3. UTF-8 validation (rejects malformed sequences).
4. `LrcParseOptions.FallbackEncoding` (default `Encoding.UTF8`); emits `LRC0010` at Error severity if it kicks in. Set `FallbackEncoding = null` to fail loudly instead.

For non-Unicode encodings without a BOM (Shift-JIS, GBK, Big5), provide the encoding explicitly — statistical detection is intentionally out of scope.

### Whitespace and empty lines

Lyric text is preserved verbatim, including whitespace-only lines and trailing whitespace. Apply display filters at the UI layer:

```csharp
foreach (var line in doc.Lines)
{
    if (line is LrcPlainLine p && string.IsNullOrWhiteSpace(p.Text)) continue;
    Render(line);
}
```

### Tolerantly accepted variants

Beyond the canonical `[mm:ss.xx]`, the parser accepts these in tolerant mode (the default), each producing an informational diagnostic:

| Variant | Diagnostic | Example |
|---|---|---|
| Three-digit fraction `mm:ss.fff` | `LRC0030` | `[00:01.500]` |
| Colon fraction separator `mm:ss:ff` | `LRC0030` | `[00:01:50]` |
| No fraction `mm:ss` | `LRC0030` | `[00:01]` |
| Hours notation `h:mm:ss.ff` | `LRC0030` | `[1:02:33.45]` |
| Comma decimal `mm:ss,ff` | `LRC0030` | `[01:23,45]` |
| ID3 language prefix `xxx\|\|...` | `LRC0092` | `eng\|\|[ti:...]` |

Set `LrcParseOptions.Strictness = LrcStrictness.Strict` to throw `LrcParseException` on the first error-severity diagnostic. Tolerant mode collects everything for inspection on `LrcParseResult.Diagnostics`.

## Comparison

ModernLrc compared to other .NET LRC parsers as of 2026-04-28.

### Performance

Throughput on synthetic basic-LRC inputs (Small ≈ 20 lines, Medium ≈ 200 lines, Large ≈ 2,000 lines). Lower is better. Ratio is relative to ModernLrc.

#### Parse

| Library                          | Small (µs)     | Medium (µs)    | Large (µs)       | Large alloc |
|----------------------------------|---------------:|---------------:|-----------------:|------------:|
| **ModernLrc 1.1.0**              | **1.89 (1.00×)** | **15.3 (1.00×)** | **154 (1.00×)**     | **324 KB**  |
| Opportunity.LrcParser 1.0.4      | 1.93 (1.02×) | 12.4 (0.81×) | 121 (0.78×) | 317 KB |
| Kfstorm.LrcParser 1.0.3          | 12.5 (6.63×) | 106 (6.92×) | 1,066 (6.91×) | 2,156 KB |
| LrcParser 2025.623.0 (karaoke-dev) | 22.4 (11.9×) | 231 (15.1×) | 2,381 (15.4×) | 5,434 KB |
| SharpLyrics 1.0.0.2 (archived)   | 192 (101×) | 277 (18.1×) | 1,152 (7.47×) | 2,252 KB |

#### Write

| Library                          | Small (µs)     | Medium (µs)    | Large (µs)       | Large alloc |
|----------------------------------|---------------:|---------------:|-----------------:|------------:|
| **ModernLrc 1.1.0**              | **2.76 (1.00×)** | **23.1 (1.00×)** | **367 (1.00×)**     | **399 KB**  |
| Opportunity.LrcParser 1.0.4      | 3.22 (1.17×) | 29.2 (1.26×) | 423 (1.15×) | 648 KB |
| LrcParser 2025.623.0 (karaoke-dev) | 5.27 (1.91×) | 49.6 (2.14×) | 635 (1.73×) | 2,013 KB |
| Kfstorm.LrcParser 1.0.3          | — | — | — | (no write API) |
| SharpLyrics 1.0.0.2 (archived)   | — | — | — | (no write API) |

ModernLrc edges Opportunity.LrcParser on small inputs and trails by 19–27% on larger inputs; the gap is the cost of its broader feature surface (recovery, structured diagnostics, voice tracking, encoding detection, word-level timing, stable sort with reorder detection). Allocation is within 2% across all sizes. ModernLrc leads on write across every input size — 13–26% faster than Opportunity.LrcParser and ~1.7–2.1× faster than karaoke-dev/LrcParser — while still emitting consecutive same-text lines as a collapsed `[t1][t2]text` group by default for round-trip fidelity.

### Features

| Capability                              | ModernLrc | Opportunity | karaoke-dev | Kfstorm | SharpLyrics |
|-----------------------------------------|:---------:|:-----------:|:-----------:|:-------:|:-----------:|
| Basic LRC `[mm:ss.xx]text`              |    ✓     |     ✓       |     ✓       |    ✓    |     ✓       |
| Enhanced LRC (word-level `<…>`)         |    ✓     |     ✗       |     ✓       |    ✗    |   partial¹  |
| Walaoke voice markers (`M:`/`F:`/`D:`)  |    ✓     |  partial²   |     ✗       |    ✗    |     ✗       |
| ID3 metadata tags (`ti`, `ar`, `al`, …) |    ✓     |     ✓       |     ✓       |    ✓    |     ✗       |
| `[offset:N]` semantics                  |    ✓     |     ✓       |     ✗       |    ✓    |     ✗       |
| ID3-style language prefix (`eng\|\|…`)  |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| Error recovery (continue past bad input)|    ✓     |  partial³   |     ✗       |    ✗    |     ✗       |
| Structured diagnostics with line/column |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| Strict / lenient parse modes            |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| Round-trip parse → write → parse        |    ✓     |     ✓       |     ✓       |    ✗    |     ✗       |
| Sync write API                          |    ✓     |     ✓       |     ✓       |    ✗    |     ✗       |
| Async parse / write                     |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| Byte / `Stream` input + encoding detect |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| `IBufferWriter<byte/char>` output       |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| `TryParse` / `TryWrite` non-throwing    |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| Span-first parser (no regex)            |    ✓     |     ✓       |     ✗       |    ✗    |     ✗       |
| Native AOT compatible                   |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| Trim-safe                               |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| Nullable reference types                |    ✓     |     ✗       |     ✓       |    ✗    |     ✓       |
| Target framework                        |  net10.0 | netstandard1.0 | netstandard2.1 | portable PCL | netstandard2.0 |
| Last release                            |  2026-04 |  2018-05    |  2025-06    | 2015-06 | 2023-11 (archived) |

¹ SharpLyrics has internal word-time extraction, but the public API surfaces only single-timestamp lines from a file path — no string input.
² Opportunity.LrcParser exposes `LineWithSpeaker` for `Speaker: text` patterns, not the Walaoke `M:`/`F:`/`D:` voice protocol with state propagation.
³ Opportunity.LrcParser collects parse exceptions in a list, but they are unstructured (no diagnostic codes or severity).

### Methodology

- Workload: synthetic basic-LRC inputs (`[mm:ss.xx]text` + ID3 metadata header), `Small` ≈ 20 lines, `Medium` ≈ 200 lines, `Large` ≈ 2,000 lines. Inputs are stored as `string` constants — no file I/O on the timed path. SharpLyrics requires a file path, so its corpus is staged to a temp file in `[GlobalSetup]` (one-time cost outside the timed region).
- Each parser called via the smallest possible idiomatic public entry point — no tuning.
- Write benchmarks start from each library's own parsed model (parsed once in `[GlobalSetup]`) so the timed region measures serialisation only, not cross-model conversion.
- Environment: BenchmarkDotNet 0.15.8, .NET 10.0.7 (SDK 10.0.202), Windows 11, AMD Ryzen 5 5600 (6 cores, 12 threads).
- Numbers will drift across machines and SDK versions — treat ratios as the reliable signal, absolute numbers as a snapshot of one machine on one day.

## Tools

### Sample CLI

```
dotnet run --project samples/ModernLrc.Samples.Console -- parse path/to/song.lrc
dotnet run --project samples/ModernLrc.Samples.Console -- shift path/to/song.lrc 500
```

### Benchmarks

```
dotnet run -c Release --project bench/ModernLrc.Benchmarks -- --filter '*'
```

Uses BenchmarkDotNet. Filter to a benchmark group with e.g. `--filter ParseBenchmarks*`.

## License

MIT — see [`LICENSE`](LICENSE).
