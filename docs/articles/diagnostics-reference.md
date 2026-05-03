# Diagnostics reference

Every recoverable issue the parser or writer encounters is reported as a
[`LrcDiagnostic`](xref:ModernLrc.Diagnostics.LrcDiagnostic) carrying a stable
code (`LRC00xx`), severity, 1-based line/column location, and a human-readable
message. Codes are exposed as constants on
[`LrcDiagnosticIds`](xref:ModernLrc.Diagnostics.LrcDiagnosticIds) so consumers
can `switch` on them without coupling to an enum that changes between versions.

In **tolerant** mode (the default), every code below is collected on
[`LrcParseResult.Diagnostics`](xref:ModernLrc.LrcParseResult.Diagnostics) and the
parser keeps going. In **strict** mode, the first Error-severity diagnostic causes
[`LrcParseException`](xref:ModernLrc.LrcParseException) to be thrown.

## Codes

### Structural

| Code | Severity | Trigger | Recovery |
|---|---|---|---|
| `LRC0001` `UnclosedTag` | Error | A `[` opens with no matching `]` before line end. | Skip rest of line. |
| `LRC0002` `InvalidTimestamp` | Error | Digit-prefixed `[…]` that fails timestamp parsing (e.g. `[00:99.00]` — seconds out of range). | Skip the bracketed group; rest of line continues. |
| `LRC0003` `MalformedIdTag` | Error | `[key]` with no `:` (e.g. `[bad]junk`). | Skip the bracketed group. |
| `LRC0004` `UnknownIdTag` | Info | `[xx:value]` where `xx` isn't one of the known keys. | Preserved verbatim in `Metadata.RawTags`. |
| `LRC0005` `InvalidOffset` | Warning | `[offset:not-a-number]`. | Tag preserved in `RawTags`; typed `Offset` unchanged. |
| `LRC0006` `InvalidLength` | Warning | `[length:invalid]`. | Tag preserved in `RawTags`; typed `Length` unchanged. |
| `LRC0007` `InvalidEnhancedTimestamp` | Error | `<bad>` inside an enhanced line (text isn't a valid timestamp). | Word skipped; following text is preserved as literal line text. |
| `LRC0008` `UnclosedEnhancedTimestamp` | Error | `<` with no matching `>` before line end. | Skip rest of line. |
| `LRC0009` `EmptyTimestamp` | Warning | Literal `[]` group. | Group dropped; subsequent timestamps still parsed. |

### Encoding

| Code | Severity | Trigger | Recovery |
|---|---|---|---|
| `LRC0010` `EncodingFallback` | **Error** | BOM detection failed and bytes are not valid UTF-8 — the configured `FallbackEncoding` was used. | Falls back; result still produced. The Error severity is intentional — fallback should be a conscious choice. |

### Metadata

| Code | Severity | Trigger | Recovery |
|---|---|---|---|
| `LRC0020` `ConflictingMetadata` | Warning | Same key encountered twice with different values. | Last-wins on the typed accessor; both entries kept in `RawTags`. |
| `LRC0021` `DuplicateMetadata` | Info | Same key encountered twice with the *same* value. | Kept once on the typed accessor; both entries in `RawTags`. |

### Timestamp variants (tolerant accept)

The parser accepts these non-canonical forms; each emits an Info diagnostic so callers
can detect them if they care.

| Code | Severity | Trigger | Example |
|---|---|---|---|
| `LRC0030` `NonStandardTimestamp` | Info | Any timestamp that is not strictly `mm:ss.xx`. | `[00:01]`, `[00:01.500]`, `[00:01:50]`, `[1:02:33.45]`, `[01:23,45]` |
| `LRC0031` `TruncatedPrecision` | Info | Sub-millisecond fractional digits supplied. Fires for any fraction separator: `.`, `,`, or colon-fraction (`mm:ss:ffff` / `h:mm:ss:ffff`). | `[00:01.12345]`, `[01:23,4567]`, `[00:23:4567]` → millisecond truncation |

### Voice markers

The parser recognises Walaoke `M:` / `F:` / `D:` prefixes and propagates voice state across
lines. There are no diagnostics emitted by the voice path today — unrecognised
prefixes are simply treated as line text.

### Lyric flow

| Code | Severity | Trigger | Recovery |
|---|---|---|---|
| `LRC0050` `DroppedUntimedText` | Info | Free text on a line with no timestamp. | Text dropped from `Lines`. |
| `LRC0051` `TimestampWithoutText` | Warning | Timestamp followed only by line terminator. | Empty `Text` line still emitted. |
| `LRC0060` `LinesReordered` | Info | Lines were not in monotonic timestamp order; output sorted. | Document is sorted on the way out. |
| `LRC0070` `ImplausibleTimestamp` | Info | Timestamp exceeds [`ImplausibleTimestampThreshold`](xref:ModernLrc.LrcParseOptions.ImplausibleTimestampThreshold) (default 24 h). | Value still parsed and kept. |
| `LRC0080` `EmptyEnhancedWord` | Info | Enhanced line had no parseable `<ts>word` pairs. | Demoted to plain line preserving verbatim text. |

### Write-side

| Code | Severity | Trigger | Recovery |
|---|---|---|---|
| `LRC0090` `UnrepresentableVoiceTransition` | Warning | A line transitions back to `LrcVoice.Default` after a non-default voice — Walaoke has no "clear voice" marker. | No marker emitted; the previously emitted voice persists in re-parsed output. Surface via [`LrcWriter.ValidateForWrite`](xref:ModernLrc.LrcWriter.ValidateForWrite*). |

### Tolerance / pragmatic

| Code | Severity | Trigger | Recovery |
|---|---|---|---|
| `LRC0091` `BracketedContentTolerance` | Info | A `[…]` group after the first that fails to parse as a timestamp. | Treated as part of the line text. |
| `LRC0092` `Id3LanguagePrefixStripped` | Info | Input begins with `xx\|\|` (2-letter ISO 639-1) or `xxx\|\|` (3-letter ISO 639-2) language code, as some streaming-service exports use. | Prefix stripped before scanning. |

### Diagnostic cap

| Code | Severity | Trigger | Recovery |
|---|---|---|---|
| `LRC0099` `MaxDiagnosticsReached` | Warning | More diagnostics fired than [`MaxDiagnostics`](xref:ModernLrc.LrcParseOptions.MaxDiagnostics) allows. | Subsequent diagnostics suppressed. Set `MaxDiagnostics = 0` to suppress everything (including this notice). |

## Recipes

### Suppress all diagnostics

```csharp
var quiet = new LrcParseOptions { MaxDiagnostics = 0 };
var result = LrcParser.Parse(text, quiet);
result.Diagnostics.Length.ShouldBe(0);
```

`Strict` mode still throws on Error-severity codes even when the cap suppresses
the diagnostic body.

### Strict-mode error handling

```csharp
var strict = new LrcParseOptions { Strictness = LrcStrictness.Strict };
try
{
    var result = LrcParser.Parse(text, strict);
}
catch (LrcParseException ex)
{
    Console.WriteLine($"{ex.FirstError?.Code} at L{ex.FirstError?.Line}");
    // ex.PartialResult holds whatever was parsed before the failure
}
```

### Triage Error / Warning / Info

```csharp
var byTier = result.Diagnostics
    .GroupBy(d => d.Severity)
    .ToDictionary(g => g.Key, g => g.Count());
```
