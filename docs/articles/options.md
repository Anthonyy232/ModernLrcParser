# Options reference

Both option records are immutable; use `with` expressions to derive variants.

## `LrcParseOptions`

| Property | Default | Notes |
|---|---|---|
| `Strictness` | `Tolerant` | `Strict` throws [`LrcParseException`](xref:ModernLrc.LrcParseException) on the first Error-severity diagnostic. |
| `Encoding` | `null` (auto-detect) | When non-null, decodes unmarked input with this encoding. A recognised BOM still wins (matches `StreamReader`); strip the BOM yourself to force a different decoding. See [encoding pipeline](encoding-pipeline.md). |
| `FallbackEncoding` | `Encoding.UTF8` | Used when BOM is absent and bytes are not valid UTF-8. Set to `null` to throw instead. |
| `MaxDiagnostics` | 256 | Cap. `0` suppresses everything (including the `LRC0099` cap notice). Strict-mode bail still works. |
| `ImplausibleTimestampThreshold` | 24 h | Above this, `LRC0070` is emitted. Set to `TimeSpan.Zero` to disable. |
| `ReadBufferSize` | 4096 | Initial read buffer for stream/file inputs. Min 64. |

```csharp
// Recipe: parse strictly, suppress all diagnostic noise.
var quietStrict = new LrcParseOptions
{
    Strictness = LrcStrictness.Strict,
    MaxDiagnostics = 0,
};
```

## `LrcWriteOptions`

| Property | Default | Notes |
|---|---|---|
| `LineEnding` | `Lf` | `Lf`, `Crlf`, `Cr`, or `System` (which falls back to `Lf` if `Environment.NewLine` is unusual). |
| `Encoding` | UTF-8 (no BOM) | Used for `Stream` / `IBufferWriter<byte>` paths. |
| `EmitByteOrderMark` | `false` | Sole BOM control. `true` prepends `Encoding.GetPreamble()`. |
| `TrailingNewline` | `true` | Whether the rendered output ends with one line ending. |
| `TimestampPrecision` | `Centiseconds` | `mm:ss.xx` vs `mm:ss.xxx` (Milliseconds). |
| `CollapseIdenticalLines` | `true` | Adjacent identical-content lines merge into one multi-timestamp entry (`[t1][t2]text`). This is the round-trip mechanism for inputs originally written as multi-timestamp groups, since the parser fans them out into one line per timestamp. Set `false` to emit one timestamp per line. |
| `EmitVoiceMarkers` | `true` | When `false`, voice metadata is dropped on render. |
| `VoiceMarkerOnChangeOnly` | `true` | When `false`, every non-default-voice line gets an explicit marker. |
| `MetadataOrdering` | `Canonical` | Ordering of the metadata block — see below. |
| `InitialBufferSize` | 4096 | Floor for the initial output buffer (chars or bytes per sink). Min 16. The string / TextWriter / TryWrite / Stream sinks may stage into a larger buffer when a document's estimated size exceeds this — eliminates copy-on-grow waste on large renders without forcing callers to tune. The `IBufferWriter<T>` sinks honor this verbatim. |

### `MetadataOrdering`

- **`Canonical`** — `ti, ar, al, au, lr, length, by, offset, re, ve` (the order
  most LRC players expect), then `RawTags` in original array order skipping
  strongly-typed duplicates.
- **`Alphabetical`** — typed accessors A→Z by C# property name (Album, Artist,
  Author, …), then `RawTags` A→Z by key.

Both orderings preserve all RawTags — the typed accessors are written from the
last-wins value and `RawTags` keep every original entry.

### `LineEnding`

`System` resolves to `Environment.NewLine` if that's `\n`, `\r\n`, or `\r`;
otherwise it falls back to `\n` to keep output predictable across exotic
hosts.

### `TrailingNewline`

When `false`, the renderer strips any trailing line endings from the output —
useful when piping into a writer that has its own newline policy.

```csharp
// Recipe: Windows-friendly output with explicit BOM.
var winFriendly = new LrcWriteOptions
{
    LineEnding = LrcLineEnding.Crlf,
    EmitByteOrderMark = true,
    Encoding = Encoding.UTF8,
};
```
