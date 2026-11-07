# ModernLrc

A fast LRC (lyrics) parser and writer for .NET 10. Native AOT compatible, trim-safe, and no dependencies outside the BCL.

Documentation and API reference: <https://anthonyy232.github.io/ModernLrcParser>

## Features

- **Full LRC syntax:** `[mm:ss.xx]text`, multi-timestamp `[t1][t2]…` lines, Enhanced LRC word timing `<…>`, and Walaoke voice markers (`M:` / `F:` / `D:`) carried forward to the lines that follow.
- **Tolerant by default:** malformed input produces diagnostics with stable codes (`LRC0001`–`LRC0099`) and line/column positions instead of exceptions. `LrcStrictness.Strict` throws on the first error.
- **Any input:** `string`, `ReadOnlySpan<char>`, `ReadOnlyMemory<char>`, `TextReader`, bytes, `Stream` and file paths, sync and async.
- **Encoding detection** for byte input: BOM, then UTF-8, then a fallback you choose (Latin-1 by default).
- **Any output:** string, `TextWriter`, `Stream`, `IBufferWriter<char>` or `IBufferWriter<byte>`, fixed spans (`TryWrite`), and atomic file writes.
- **Playback helpers:** `FindLineAt` and `LinesInRange` look up lines by playback position, applying `[offset:N]`.
- **Round trips:** writing a parsed document and parsing it again gives an equal document, keeping the original `[offset:N]` tag. Timestamps are written as `mm:ss.xx` by default, so millisecond timestamps need millisecond precision to survive. `LrcWriter.ValidateForWrite` reports anything that can't be written back.

## Install

```sh
dotnet add package ModernLrc
```

Requires a project targeting .NET 10 (`net10.0`).

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

## Good to know

- Timestamps are kept as written. `[offset:N]` goes to `Metadata.Offset`, and `GetEffectiveTime`, `FindLineAt` and `LinesInRange` apply it. See [Playback](https://anthonyy232.github.io/ModernLrcParser/articles/playback.html).
- Byte input with no BOM that isn't valid UTF-8 is decoded with `FallbackEncoding` (Latin-1 by default) and reported as `LRC0010`. Legacy encodings such as Shift-JIS or GBK aren't detected: set `LrcParseOptions.Encoding`. See [Encoding pipeline](https://anthonyy232.github.io/ModernLrcParser/articles/encoding-pipeline.html).
- Lyric text is kept verbatim, including whitespace-only lines and trailing spaces. Filter them when you display lyrics.
- Common non-standard forms such as `[00:01]`, `[00:01.500]`, `[1:02:33.45]`, `[01:23,45]` and an `eng||` language prefix are accepted with informational diagnostics. See the [diagnostics reference](https://anthonyy232.github.io/ModernLrcParser/articles/diagnostics-reference.html).

## Comparison

ModernLrc 2.0.1 compared with other .NET LRC libraries on 2026-10-13.

### Performance

Mean time on synthetic basic LRC (Small ≈ 20 lines, Medium ≈ 200, Large ≈ 2,000). Lower is better; the ratio is relative to ModernLrc.

#### Parse

| Library                          | Small (µs)     | Medium (µs)    | Large (µs)       | Large alloc |
|----------------------------------|---------------:|---------------:|-----------------:|------------:|
| **ModernLrc**                    | **0.93 (1.00×)** | **6.51 (1.00×)** | **61.5 (1.00×)**   | **331 KB**  |
| Opportunity.LrcParser 1.0.4      | 1.25 (1.34×) | 9.15 (1.41×) | 130 (2.12×) | 317 KB |
| Kfstorm.LrcParser 1.0.3          | 7.23 (7.75×) | 58.8 (9.03×) | 579 (9.42×) | 2,156 KB |
| LrcParser 2025.623.0 (karaoke-dev) | 13.7 (14.7×) | 132 (20.3×) | 1,398 (22.7×) | 5,434 KB |
| SharpLyrics 1.0.0.2 (archived)   | 109 (117×) | 174 (26.7×) | 848 (13.8×) | 2,252 KB |

#### Write

| Library                          | Small (µs)     | Medium (µs)    | Large (µs)       | Large alloc |
|----------------------------------|---------------:|---------------:|-----------------:|------------:|
| **ModernLrc**                    | **1.99 (1.00×)** | **15.0 (1.00×)** | **181 (1.00×)**    | **197 KB**  |
| Opportunity.LrcParser 1.0.4      | 1.83 (0.96×) | 17.0 (1.15×) | 246 (1.36×) | 648 KB |
| LrcParser 2025.623.0 (karaoke-dev) | 4.31 (2.25×) | 31.1 (2.10×) | 446 (2.46×) | 2,013 KB |
| Kfstorm.LrcParser 1.0.3          | — | — | — | (no write API) |
| SharpLyrics 1.0.0.2 (archived)   | — | — | — | (no write API) |

ModernLrc parses 1.3–2.1× faster than Opportunity.LrcParser, the next fastest, with about 5% more allocation. It writes medium and large inputs 1.15–1.36× faster with about a third of the allocation; Opportunity.LrcParser is slightly faster on small inputs.

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
| Native AOT compatible                   |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| Trim-safe                               |    ✓     |     ✗       |     ✗       |    ✗    |     ✗       |
| Target framework                        |  net10.0 | netstandard1.0 | netstandard2.1 | portable PCL | netstandard2.0 |
| Last release                            |  2026-10 |  2018-05    |  2025-06    | 2015-06 | 2023-11 (archived) |

¹ SharpLyrics extracts word timing internally, but its public API only returns single-timestamp lines, and only from a file path.
² Opportunity.LrcParser has `LineWithSpeaker` for `Speaker: text`, not Walaoke `M:`/`F:`/`D:` markers carried across lines.
³ Opportunity.LrcParser collects parse exceptions in a list, without codes or severity.

### Methodology

- Inputs are `string` constants of basic LRC with an ID3 header, so no file I/O is timed. SharpLyrics only reads files, so its input is written to a temp file during setup.
- Each library is called through its simplest public entry point, untuned. Write benchmarks start from the library's own parsed model, so only serialization is timed.
- BenchmarkDotNet 0.15.8, .NET 10.0.12, Windows 11, AMD Ryzen 7 9850X3D. Absolute numbers vary by machine; compare the ratios.
- The harness is on the [`bench/comparison`](https://github.com/Anthonyy232/ModernLrcParser/tree/bench/comparison) branch.

## Contributing

Contributions are welcome. Changes are made with coding agents: point yours at [AGENTS.md](https://github.com/Anthonyy232/ModernLrcParser/blob/main/AGENTS.md), which covers the conventions and the checks a change must pass.

## License

[MIT](https://github.com/Anthonyy232/ModernLrcParser/blob/main/LICENSE)
