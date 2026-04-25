# Getting started

## Install

```
dotnet add package ModernLrc
```

Requires .NET 10 SDK.

## Parse

The canonical entry is `LrcParser.Parse(string)`. The result always carries a
`Document` plus every `Diagnostic` that was emitted during the scan.

```csharp
using ModernLrc;
using ModernLrc.Model;

const string source = """
[ti:Demo Song]
[ar:Tester]
[00:01.00]intro line
[00:05.20]F: female voice
[00:08.50]<00:08.50>word <00:09.00>by <00:09.50>word
""";

var result = LrcParser.Parse(source);

Console.WriteLine(result.Document.Metadata.Title);    // "Demo Song"
Console.WriteLine(result.Document.Lines.Count);       // 3
Console.WriteLine(result.HasErrors);                  // false
```

### Inspecting lines

```csharp
foreach (var line in result.Document.Lines)
{
    var t = line.Timestamps[0];
    switch (line)
    {
        case LrcPlainLine plain:
            Console.WriteLine($"[{t}] {plain.EffectiveVoice}: {plain.Text}");
            break;

        case LrcEnhancedLine enhanced:
            foreach (var word in enhanced.Words)
                Console.Write($"<{word.Timestamp}>{word.Text}");
            Console.WriteLine();
            break;
    }
}
```

### Inspecting diagnostics

Every recoverable issue is recorded — even on `HasErrors == false` you can find
informational notes (e.g. `LRC0030` for non-standard timestamp variants):

```csharp
foreach (var d in result.Diagnostics)
    Console.WriteLine($"{d.Code} {d.Severity} L{d.Line}:C{d.Column} — {d.Message}");
```

See the [diagnostics reference](diagnostics-reference.md) for the full code catalogue.

## Author

```csharp
var doc = new LrcDocumentBuilder()
    .WithTitle("New Song")
    .WithArtist("Artist")
    .WithOffset(TimeSpan.FromMilliseconds(-200))
    .AddLine("00:01.00", "first line")
    .AddLine("00:05.00", "she sings", LrcVoice.Female)
    .Build();
```

The builder is mutable and chainable. `Build()` is idempotent — call it as many
times as you want; it doesn't reset the builder's state.

## Write

```csharp
// To a string
string lrc = LrcWriter.Write(doc);

// To a stream (UTF-8 by default; takes a fast path that avoids any intermediate string)
using var fs = File.Create("song.lrc");
LrcWriter.Write(doc, fs);

// Atomic file write (temp + rename)
LrcWriter.WriteFile(doc, "song.lrc");

// Zero-allocation: render straight into your own buffer writer
var bw = new System.Buffers.ArrayBufferWriter<byte>();
LrcWriter.Write(doc, bw);
ReadOnlySpan<byte> bytes = bw.WrittenSpan;
```

## Round-trip

The parser and writer are designed to round-trip losslessly for the supported
grammar (modulo whitespace inside the line that wasn't normalized at parse time).
Specifically: voice markers, multi-timestamp lines, enhanced word timing,
metadata block, and the verbatim `[offset:N]` tag all survive a parse → write
cycle.

```csharp
var doc1 = LrcParser.Parse(source).Document;
var rendered = LrcWriter.Write(doc1);
var doc2 = LrcParser.Parse(rendered).Document;
// doc1.Lines structurally equal doc2.Lines
```

## Next steps

- [Encoding pipeline](encoding-pipeline.md) — non-UTF-8 input, BOM behaviour
- [Async and cancellation](async-and-cancellation.md) — large files, off-UI-thread
- [Playback](playback.md) — `FindLineAt`, `LinesInRange`, the offset semantics
- [Options reference](options.md) — every option on `LrcParseOptions` / `LrcWriteOptions`
- [Diagnostics reference](diagnostics-reference.md) — what every `LRC00xx` code means
