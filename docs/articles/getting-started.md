# Getting started

## Install

```
dotnet add package ModernLrc
```

Requires a project targeting .NET 10 (`net10.0`).

## Parse

`LrcParser.Parse` returns an [`LrcParseResult`](xref:ModernLrc.LrcParseResult): the
`Document` and the `Diagnostics` reported while parsing. Other overloads take a span,
`ReadOnlyMemory<char>`, `TextReader`, bytes, a `Stream`, or a file path (`ParseFile`).

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

Console.WriteLine(result.Document.Metadata.Title);    // Demo Song
Console.WriteLine(result.Document.Lines.Count);       // 3
Console.WriteLine(result.HasErrors);                  // False
```

A line with several timestamps (`[t1][t2]text`) becomes one line per timestamp, and
lines are sorted by timestamp.

### Lines

Each line is either an `LrcPlainLine` or, when it has `<…>` word timing, an
`LrcEnhancedLine`.

```csharp
foreach (var line in result.Document.Lines)
{
    switch (line)
    {
        case LrcPlainLine plain:
            Console.WriteLine($"[{line.Timestamp}] {line.EffectiveVoice}: {plain.Text}");
            break;

        case LrcEnhancedLine enhanced:
            foreach (var word in enhanced.Words)
                Console.Write($"<{word.Timestamp}>{word.Text}");
            Console.WriteLine();
            break;
    }
}
```

`line.GetText()` gives the text of either kind.

### Diagnostics

By default, malformed input doesn't throw. Each problem is reported with a code, severity and
position, and parsing continues. Info diagnostics (for example `LRC0030` for a
non-standard timestamp such as `[00:01]`) can appear even when `HasErrors` is false.

```csharp
foreach (var d in result.Diagnostics)
    Console.WriteLine($"{d.Code} {d.Severity} L{d.Line}:C{d.Column} {d.Message}");
```

See the [diagnostics reference](diagnostics-reference.md) for every code and for
strict mode.

## Author and edit

```csharp
var doc = new LrcDocumentBuilder()
    .WithTitle("New Song")
    .WithArtist("Artist")
    .WithOffset(TimeSpan.FromMilliseconds(-200))
    .AddLine("00:01.00", "first line")
    .AddLine("00:05.00", "she sings", LrcVoice.Female)
    .Build();
```

`Build()` sorts the lines and leaves the builder unchanged, so you can keep adding
lines and build again. To edit a parsed document, seed a builder with it
(`new LrcDocumentBuilder(parsed)`) or use a `with` expression; documents are
immutable records.

## Write

```csharp
// To a string
string lrc = LrcWriter.Write(doc);

// To a stream (UTF-8 by default)
using (var fs = File.Create("song.lrc"))
    LrcWriter.Write(doc, fs);

// To a file, atomically (temp file + rename)
LrcWriter.WriteFile(doc, "song.lrc");

// Into your own char buffer
var bw = new System.Buffers.ArrayBufferWriter<char>();
LrcWriter.Write(doc, bw);
ReadOnlySpan<char> chars = bw.WrittenSpan;
```

`TryWrite` renders into a fixed `Span<char>` or `Span<byte>` and returns `false` if it
is too small. Formatting, encoding and line endings are set with
[`LrcWriteOptions`](options.md#lrcwriteoptions).

## Async and cancellation

The `Stream`, `TextReader`/`TextWriter` and file overloads have async versions that
take a `CancellationToken`:

```csharp
var parsed = await LrcParser.ParseFileAsync("song.lrc", cancellationToken: ct);

using (var stream = File.OpenRead("song.lrc"))
    parsed = await LrcParser.ParseAsync(stream, cancellationToken: ct);

await LrcWriter.WriteFileAsync(doc, "song.lrc", cancellationToken: ct);
```

The token is checked before any I/O and passed to the underlying read or write. A
token that is already cancelled throws `OperationCanceledException` without doing any
I/O. Parsing and rendering themselves run synchronously and aren't cancelled once
started.

## Round trips

Writing a parsed document and parsing the output gives an equal document, apart from
millisecond timestamps (see below). Voice markers, multi-timestamp lines, word timing,
metadata (including duplicate and unknown tags) and the original `[offset:N]` tag are
kept.

```csharp
var doc1 = LrcParser.Parse(source).Document;
var doc2 = LrcParser.Parse(LrcWriter.Write(doc1)).Document;
Console.WriteLine(doc1 == doc2);   // True
```

The file isn't reproduced byte for byte: timestamps are written in the standard form
and untimed text is dropped. Beyond that:

- Timestamps are written as `mm:ss.xx` by default, so `[00:01.505]` comes back as
  `[00:01.50]`. Set `TimestampPrecision = LrcTimestampPrecision.Milliseconds` to keep
  milliseconds.
- In documents you build or edit, lyric text that looks like LRC syntax (such as
  `<00:01.00>` or a leading `F: `) is read as syntax when parsed again.
- A line can't go back to the default voice after an `M:`/`F:`/`D:` line; see
  `LRC0090` in the [diagnostics reference](diagnostics-reference.md#write-side).
