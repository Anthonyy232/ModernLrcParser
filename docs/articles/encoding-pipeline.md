# Encoding pipeline

Byte input (`ReadOnlySpan<byte>`, `byte[]`, `Stream`, file path) is decoded in this order:

1. **BOM.** A UTF-8 (`EF BB BF`), UTF-16 LE (`FF FE`) or UTF-16 BE (`FE FF`) byte order
   mark selects that encoding, and the BOM is dropped.
2. **Your encoding.** If [`LrcParseOptions.Encoding`](xref:ModernLrc.LrcParseOptions.Encoding)
   is set, it is used. A BOM still wins, as with `StreamReader`; strip it yourself to
   force a different encoding.
3. **UTF-8.** Valid UTF-8 is decoded as UTF-8.
4. **Fallback.** Anything else is decoded with
   [`FallbackEncoding`](xref:ModernLrc.LrcParseOptions.FallbackEncoding), Latin-1 by
   default, and reported as [`LRC0010`](diagnostics-reference.md#encoding) with Error
   severity. If `FallbackEncoding` is `null`, `LrcParseException` is thrown instead.

Latin-1 maps every byte to a character, so decoding never fails and nothing becomes
U+FFFD. Western European text (for example a Windows-1252 `é`) comes through intact.
Because `LRC0010` is an Error, strict mode throws on it.

## Legacy code pages

Shift-JIS, GBK, Big5, EUC-KR and other legacy encodings aren't detected. Set
`Encoding` to the file's encoding. .NET only includes ASCII, Latin-1 and the Unicode
encodings until you register the code pages provider, once at startup:

```csharp
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var options = new LrcParseOptions { Encoding = Encoding.GetEncoding("GBK") };
var result = LrcParser.ParseFile("song.lrc", options);
```

## Throw instead of falling back

```csharp
var noFallback = new LrcParseOptions { FallbackEncoding = null };
try
{
    LrcParser.Parse(bytes, noFallback);
}
catch (LrcParseException)
{
    // No BOM, and the bytes aren't valid UTF-8.
}
```

## Writing

Byte outputs (`Stream`, files, `IBufferWriter<byte>`, `Span<byte>`) use
[`LrcWriteOptions.Encoding`](xref:ModernLrc.LrcWriteOptions.Encoding), UTF-8 by default.
A BOM is written only when
[`EmitByteOrderMark`](xref:ModernLrc.LrcWriteOptions.EmitByteOrderMark) is `true`.
