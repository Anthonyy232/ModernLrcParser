# Diagnostics reference

Each problem the parser finds is reported as an
[`LrcDiagnostic`](xref:ModernLrc.Diagnostics.LrcDiagnostic) with a stable code, a
severity, a 1-based line and column, and a message. The codes are constants on
[`LrcDiagnosticIds`](xref:ModernLrc.Diagnostics.LrcDiagnosticIds), so you can `switch`
on them.

By default (tolerant mode) parsing continues and diagnostics are collected on
[`LrcParseResult.Diagnostics`](xref:ModernLrc.LrcParseResult.Diagnostics).
`HasErrors` and `HasWarnings` tell you whether any Error or Warning was reported.

## Codes

### Structure

| Code | Severity | Trigger | Result |
|---|---|---|---|
| `LRC0001` `UnclosedTag` | Error | A `[` with no `]` on the same line. | Line dropped. |
| `LRC0002` `InvalidTimestamp` | Error | The first `[…]` on a line is neither a metadata tag nor a valid timestamp, e.g. `[00:99.00]` (seconds out of range). | Group skipped. Later timestamps on the line still count; with none, the text is dropped (`LRC0050`). |
| `LRC0003` `MalformedIdTag` | Error | A metadata tag with no `:`, e.g. `[bad]`. | Line dropped. |
| `LRC0004` `UnknownIdTag` | Info | A metadata tag with an unknown key, e.g. `[foo:1]`. | Kept in `Metadata.RawTags` and written back. |
| `LRC0005` `InvalidOffset` | Warning | An `[offset:…]` value that isn't a whole number of milliseconds. | Kept in `RawTags`; `Offset` isn't set from it. |
| `LRC0006` `InvalidLength` | Warning | A `[length:…]` value that isn't a valid time. | Kept in `RawTags`; `Length` isn't set from it. |
| `LRC0007` `InvalidEnhancedTimestamp` | Error | A `<…>` word timestamp that isn't valid, e.g. `<zz>`. | After a valid word, the rest of the line is kept as that word's text; otherwise the tag is skipped. |
| `LRC0008` `UnclosedEnhancedTimestamp` | Error | A `<` with no `>` on the same line. | Line dropped. |
| `LRC0009` `EmptyTimestamp` | Warning | An empty `[]`. | Ignored; other timestamps on the line still count. |

### Encoding

| Code | Severity | Trigger | Result |
|---|---|---|---|
| `LRC0010` `EncodingFallback` | Error | Byte input with no BOM that isn't valid UTF-8, decoded with `FallbackEncoding`. | Parsing continues on the decoded text. See [Encoding pipeline](encoding-pipeline.md). |

### Metadata

| Code | Severity | Trigger | Result |
|---|---|---|---|
| `LRC0020` `ConflictingMetadata` | Warning | A known tag repeated with a different value. | The last value sets the typed property; all copies stay in `RawTags`. |
| `LRC0021` `DuplicateMetadata` | Info | A known tag repeated with the same value. | All copies stay in `RawTags`. |

### Timestamps

| Code | Severity | Trigger | Result |
|---|---|---|---|
| `LRC0030` `NonStandardTimestamp` | Info | A timestamp not in `mm:ss.xx` form: `[00:01]`, `[00:01.500]`, `[00:01:50]`, `[1:02:33.45]`, `[01:23,45]`. | Accepted. |
| `LRC0031` `TruncatedPrecision` | Info | More than three fraction digits, e.g. `[00:01.12345]` or `[00:23:4567]`. | Truncated to milliseconds. |
| `LRC0070` `ImplausibleTimestamp` | Info | A timestamp above [`ImplausibleTimestampThreshold`](xref:ModernLrc.LrcParseOptions.ImplausibleTimestampThreshold) (24 hours by default). | Kept. |

### Lines

| Code | Severity | Trigger | Result |
|---|---|---|---|
| `LRC0050` `DroppedUntimedText` | Info | Text on a line with no timestamp. | Dropped. |
| `LRC0051` `TimestampWithoutText` | Warning | A timestamp with nothing after it. | Kept as a line with empty text. |
| `LRC0060` `LinesReordered` | Info | Lines not in time order. | Sorted; lines with equal timestamps keep their order. |
| `LRC0080` `EmptyEnhancedWord` | Info | A line with `<…>` tags but no valid word timestamp. | Kept as a plain line with its text as written. |
| `LRC0091` `BracketedContentTolerance` | Info | A `[…]` after a timestamp that isn't a timestamp, e.g. `[00:01.00][chorus]text`. | Kept as part of the text. |
| `LRC0092` `Id3LanguagePrefixStripped` | Info | Input starting with a 2- or 3-letter language code and `\|\|`, e.g. `eng\|\|`. | Prefix removed. |

Voice markers (`M: `, `F: `, `D: `) produce no diagnostics. A prefix that doesn't
match, such as `F:` without a space, is part of the text.

### Write side

| Code | Severity | Trigger | Result |
|---|---|---|---|
| `LRC0090` `UnrepresentableVoiceTransition` | Warning | A default-voice line follows a line with a voice. LRC has no marker that clears the voice. | Written without a marker, so it takes the previous voice when parsed again. |

The writer doesn't report diagnostics itself. Call
[`LrcWriter.ValidateForWrite`](xref:ModernLrc.LrcWriter.ValidateForWrite*) before writing
to get them; it reports nothing when `EmitVoiceMarkers` is false.

### Diagnostic cap

| Code | Severity | Trigger | Result |
|---|---|---|---|
| `LRC0099` `MaxDiagnosticsReached` | Warning | More diagnostics than [`MaxDiagnostics`](xref:ModernLrc.LrcParseOptions.MaxDiagnostics) (256 by default). | Later diagnostics are dropped. `MaxDiagnostics = 0` drops all of them, including this one. |

## Strict mode and exceptions

With `Strictness = LrcStrictness.Strict`, the first Error diagnostic throws
[`LrcParseException`](xref:ModernLrc.LrcParseException), even if `MaxDiagnostics`
kept it out of the list.

```csharp
var strict = new LrcParseOptions { Strictness = LrcStrictness.Strict };
try
{
    var result = LrcParser.Parse(text, strict);
}
catch (LrcParseException ex)
{
    Console.WriteLine($"{ex.FirstError?.Code} at L{ex.FirstError?.Line}");
    // ex.PartialResult holds what was parsed before the error.
}
```

`LrcParseException` is also thrown when byte input can't be decoded because
`FallbackEncoding` is `null`; `PartialResult` and `FirstError` are then `null`.
`ParseFile` and `ParseFileAsync` set its `FilePath`. I/O errors and
`OperationCanceledException` are not wrapped.

To just check whether text parses, `LrcParser.TryParse` returns `false` when any Error
diagnostic is reported.
