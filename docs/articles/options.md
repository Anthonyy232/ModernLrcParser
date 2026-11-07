# Options reference

Both option types are immutable records; derive variants with `with`.

## `LrcParseOptions`

| Property | Default | Notes |
|---|---|---|
| `Strictness` | `Tolerant` | `Strict` throws [`LrcParseException`](xref:ModernLrc.LrcParseException) on the first Error diagnostic. |
| `Encoding` | `null` | Encoding for byte input with no BOM. `null` detects UTF-8 and otherwise falls back. See [Encoding pipeline](encoding-pipeline.md). |
| `FallbackEncoding` | `Encoding.Latin1` | Used when byte input has no BOM and isn't valid UTF-8. `null` throws instead. |
| `MaxDiagnostics` | `256` | Diagnostics after this many are dropped (`LRC0099`). `0` drops all; strict mode still throws. |
| `ImplausibleTimestampThreshold` | 24 hours | Timestamps above it get `LRC0070`. `TimeSpan.Zero` turns the check off. |

## `LrcWriteOptions`

| Property | Default | Notes |
|---|---|---|
| `LineEnding` | `Lf` | `Lf`, `Crlf`, `Cr`, or `System` (`Environment.NewLine`, or `Lf` if that is something else). |
| `Encoding` | UTF-8 | Used by byte outputs: `Stream`, files, `IBufferWriter<byte>`, `Span<byte>`. |
| `EmitByteOrderMark` | `false` | Writes the encoding's BOM when `true`. |
| `TrailingNewline` | `true` | Ends the output with a line ending. |
| `TimestampPrecision` | `Centiseconds` | `Centiseconds` writes `mm:ss.xx`, `Milliseconds` writes `mm:ss.xxx`. |
| `CollapseIdenticalLines` | `true` | Writes consecutive lines with the same content and voice as one `[t1][t2]text` line. The parser splits such lines into one per timestamp, so this restores them. |
| `EmitVoiceMarkers` | `true` | `false` writes no `M:`/`F:`/`D:` markers. |
| `VoiceMarkerOnChangeOnly` | `true` | `false` writes a marker on every line that has a voice, not only when it changes. |
| `MetadataOrdering` | `Canonical` | Order of the metadata tags; see below. |

```csharp
var windows = new LrcWriteOptions
{
    LineEnding = LrcLineEnding.Crlf,
    EmitByteOrderMark = true,
};
```

### Metadata

The typed properties on `LrcMetadata` (`Title`, `Artist`, …) decide what is written.
Raw tags with a known key (`ti`, `length`, …) are written as they were only while
they still give the typed value, so unchanged parsed metadata keeps duplicate,
conflicting or unparseable tags such as `[length:abc]`. Once you change a typed
property, its raw tags are dropped and the new value is written. Setting it to `null`
removes the tag.

```csharp
var doc = LrcParser.Parse("[ti:Old]\n[xx:custom]\n[00:01.00]hi").Document;
var edited = doc with { Metadata = doc.Metadata with { Title = "New" } };
LrcWriter.Write(edited);   // "[ti:New]\n[xx:custom]\n\n[00:01.00]hi\n"
```

`MetadataOrdering` sets the order:

- **`Canonical`**: typed values not written from raw tags, in the order
  `ti, ar, al, au, lr, length, by, offset, re, ve`, then the raw tags in their
  original order.
- **`Alphabetical`**: typed values not written from raw tags, A to Z by property
  name (`Album`, `Artist`, `Author`, …), then the raw tags A to Z by key.
