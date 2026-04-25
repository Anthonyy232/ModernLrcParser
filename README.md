# ModernLrc

Modern, performance-first LRC (lyrics) parser and writer for .NET 10. AOT-compatible, trim-safe, zero non-BCL runtime dependencies.

📖 **Full documentation, API reference, and topical guides:** <https://anthonyy232.github.io/ModernLrcParser>
(also browseable in the [`docs/`](docs/) folder).

## Features

- **Full LRC support** — simple `[mm:ss.xx]text`, multi-timestamp `[t1][t2]…`, Enhanced LRC word timing `<…>`, Walaoke voice markers (`M:` / `F:` / `D:`) with state propagation.
- **Tolerant by default** with rich diagnostics; opt-in `LrcStrictness.Strict` for fail-fast behaviour.
- **Span-first hand-rolled scanner** — no regex, no backtracking, zero per-token transient allocations.
- **Every input shape** — `string`, `ReadOnlySpan<char>`, `ReadOnlyMemory<char>`, `TextReader`, `ReadOnlySpan<byte>`, `byte[]`, `Stream`, file paths; sync and async.
- **Zero-allocation writer** via `IBufferWriter<char>` / `IBufferWriter<byte>`; the `Write → string`, `TextWriter`, `TryWrite`, and `Stream` sinks pre-size their staging buffer to the document's estimated size so large renders don't accumulate copy-on-grow waste; atomic file write via temp + rename.
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
TimeSpan effective = doc.GetEffectiveTime(line.Timestamps[0]);
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
