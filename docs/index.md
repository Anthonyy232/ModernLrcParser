# ModernLrc

Modern, performance-first LRC (lyrics) parser and writer for **.NET 10**.
AOT-compatible, trim-safe, zero non-BCL runtime dependencies.

## Quick start

```csharp
using ModernLrc;
using ModernLrc.Model;

// Parse
var result = LrcParser.Parse("[ti:Demo]\n[00:01.00]hello\n");
Console.WriteLine(result.Document.Metadata.Title);   // "Demo"

// Author + write
var doc = new LrcDocumentBuilder()
    .WithTitle("My Song")
    .AddLine("00:12.00", "first line")
    .AddLine("00:14.20", "second line", LrcVoice.Female)
    .Build();
string lrc = LrcWriter.Write(doc);
```

## Where to next

| If you want to… | Read |
|---|---|
| Get hands-on quickly | [Getting started](articles/getting-started.md) |
| Understand every diagnostic code | [Diagnostics reference](articles/diagnostics-reference.md) |
| Handle non-UTF-8 input correctly | [Encoding pipeline](articles/encoding-pipeline.md) |
| Build a karaoke / lyric-display app | [Playback](articles/playback.md) |
| Do parse / write off the UI thread | [Async and cancellation](articles/async-and-cancellation.md) |
| Tune output formatting | [Options reference](articles/options.md) |
| Browse every public type and method | [API reference](xref:ModernLrc) |

## Highlights

- **Full LRC support** — `[mm:ss.xx]text`, multi-timestamp `[t1][t2]…`, Enhanced LRC
  word timing `<…>`, Walaoke voice markers (`M:` / `F:` / `D:`) with state propagation.
- **Tolerant by default** — every recoverable concern surfaces as a stable diagnostic
  code (`LRC0001`–`LRC0099`); opt into strict mode for fail-fast behaviour.
- **Span-first scanner** — no regex, no backtracking, zero per-token transient
  allocations; SIMD-friendly hot path.
- **Every input shape** — `string`, `ReadOnlySpan<char>`, `ReadOnlyMemory<char>`,
  `TextReader`, `ReadOnlySpan<byte>`, `byte[]`, `Stream`, file paths; sync and async.
- **Zero-allocation writer** via `IBufferWriter<char>` / `IBufferWriter<byte>`;
  `string.Create`-style single-allocation `Write → string`; atomic file write
  via temp + rename.
- **Encoding pipeline** — BOM detection (UTF-8, UTF-16 LE/BE) → caller override →
  UTF-8 validation → caller-supplied fallback.
