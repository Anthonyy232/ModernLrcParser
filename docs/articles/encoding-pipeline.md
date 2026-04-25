# Encoding pipeline

When you hand the parser bytes (`ReadOnlySpan<byte>`, `byte[]`, `Stream`, file path),
it runs a four-step pipeline to decide how to decode them:

1. **BOM detection.** UTF-8 (`EF BB BF`), UTF-16 LE (`FF FE`), UTF-16 BE (`FE FF`).
   If a BOM is found, that encoding is used and the BOM bytes are stripped.
2. **Caller override.** [`LrcParseOptions.Encoding`](xref:ModernLrc.LrcParseOptions.Encoding)
   wins over auto-detection of unmarked input if set. (BOM detection in step 1 still wins
   over this — matching `StreamReader`. Strip the BOM bytes yourself to force a different
   decoding.)
3. **UTF-8 validation.** The bytes are checked with `Utf8.IsValid`. If valid,
   they're decoded as UTF-8.
4. **Fallback.** If none of the above produced a string and
   [`FallbackEncoding`](xref:ModernLrc.LrcParseOptions.FallbackEncoding) is non-null,
   it's used and the parser emits diagnostic
   [`LRC0010 EncodingFallback`](diagnostics-reference.md#encoding) at **Error severity**
   (the Error tier is intentional — falling back should be a conscious choice).
   If `FallbackEncoding` is null, [`LrcParseException`](xref:ModernLrc.LrcParseException)
   is thrown.

## Defaults

| Option | Default |
|---|---|
| `Encoding` | `null` (auto-detect) |
| `FallbackEncoding` | `Encoding.UTF8` |

So the out-of-the-box behaviour is: BOM → UTF-8 strict → UTF-8 fallback (which always
succeeds — Latin-1 bytes get replaced with the replacement char). Most files round-trip
correctly.

## Non-Unicode codepages

For Shift-JIS, GBK, Big5, EUC-KR, and similar legacy codepages with no BOM,
**you must pass the encoding explicitly** — statistical detection is intentionally
out of scope for this library.

```csharp
// Decode a GBK-encoded Chinese LRC file.
var options = new LrcParseOptions
{
    Encoding = System.Text.Encoding.GetEncoding("GBK"),
};
var result = LrcParser.Parse(File.ReadAllBytes("song.lrc"), options);
```

To use codepages other than the ASCII / Latin / Unicode set baked into .NET,
add the `System.Text.Encoding.CodePages` package and call
`Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` once at startup.

## Fail loud on encoding ambiguity

Sometimes you'd rather throw than silently decode garbage:

```csharp
var failLoud = new LrcParseOptions { FallbackEncoding = null };
try
{
    LrcParser.Parse(suspectBytes, failLoud);
}
catch (LrcParseException)
{
    // BOM was absent and bytes are not valid UTF-8.
}
```

## Streaming-service exports

Some lyrics services prefix their LRC output with an ISO 639-1 (2-letter) or
ISO 639-2 (3-letter) language code: `en||[ti:Title]…` or `eng||[ti:Title]…`.
The parser detects either form and strips it, emitting
[`LRC0092 Id3LanguagePrefixStripped`](diagnostics-reference.md#tolerance--pragmatic)
so callers can audit the input shape if needed.

## Write side

The writer's encoding is configured via
[`LrcWriteOptions.Encoding`](xref:ModernLrc.LrcWriteOptions.Encoding) (default
UTF-8 *no BOM*) and
[`LrcWriteOptions.EmitByteOrderMark`](xref:ModernLrc.LrcWriteOptions.EmitByteOrderMark)
(sole BOM control — set `true` to prepend the encoding's preamble; set `false`
to skip it regardless of the encoding's natural BOM).

When the encoding is UTF-8, the stream and `IBufferWriter<byte>` paths take a
fast path that renders directly into bytes — no intermediate string allocation.
