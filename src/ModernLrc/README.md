# ModernLrc

A fast LRC (lyrics) parser and writer for .NET 10. Native AOT compatible, trim-safe, and no dependencies outside the BCL.

- Basic LRC, multi-timestamp lines, Enhanced LRC word timing and Walaoke voice markers (`M:` / `F:` / `D:`).
- Malformed input produces diagnostics with stable codes and line/column positions instead of exceptions.
- Reads strings, spans, `TextReader`, bytes, streams and files, sync and async, with encoding detection.
- `FindLineAt` and `LinesInRange` look up lines by playback position, applying `[offset:N]`.
- Writing a document and parsing it again gives an equal document, within the [round-trip limits](https://anthonyy232.github.io/ModernLrcParser/articles/getting-started.html#round-trips) (timestamps are written as `mm:ss.xx` by default).

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

Documentation and API reference: <https://anthonyy232.github.io/ModernLrcParser/>

Source, benchmarks and issues: <https://github.com/Anthonyy232/ModernLrcParser>

Licensed under the GNU General Public License v3.0 only. Versions up to 2.0.2 were released under the MIT License.
