# Async and cancellation

Every I/O-bound entry point has an async overload. The CPU-bound parse and write
paths are sync — the async overloads exist purely to avoid blocking on stream
reads / writes.

## Parse

```csharp
// From a stream
using var fs = File.OpenRead("song.lrc");
var result = await LrcParser.ParseAsync(fs, cancellationToken: ct);

// From a file path (async file open)
var result = await LrcParser.ParseFileAsync("song.lrc", cancellationToken: ct);

// From a TextReader
using var reader = new StringReader(source);
var result = await LrcParser.ParseAsync(reader, cancellationToken: ct);
```

`ParseAsync(Stream)` takes a fast path when the input is a `MemoryStream` whose
backing buffer is exposed (`TryGetBuffer`); otherwise it copies through an
internal `MemoryStream` then parses synchronously over the resulting span.

## Write

```csharp
// To a stream — UTF-8 takes a zero-intermediate-string fast path
using var fs = File.Create("song.lrc");
await LrcWriter.WriteAsync(doc, fs, cancellationToken: ct);

// Atomic file write (temp + rename; cleanup runs only on failure)
await LrcWriter.WriteFileAsync(doc, "song.lrc", cancellationToken: ct);

// To a TextWriter
using var sw = new StringWriter();
await LrcWriter.WriteAsync(doc, sw, cancellationToken: ct);
```

## Cancellation contract

- The token is checked with `ThrowIfCancellationRequested()` before any I/O begins.
- `Stream.CopyToAsync` and `Stream.WriteAsync` honour the token.
- `TextReader.ReadToEndAsync(CancellationToken)` honours the token.
- The synchronous parse/render loop after the I/O finishes does **not** poll the
  token — once bytes are in memory, the parse completes (typical files: sub-millisecond).

## Pre-cancelled token

If the token is already cancelled when the call is made, the method throws
`OperationCanceledException` immediately without doing any I/O.

```csharp
using var cts = new CancellationTokenSource();
await cts.CancelAsync();
await Should.ThrowAsync<OperationCanceledException>(
    () => LrcParser.ParseAsync(stream, cancellationToken: cts.Token).AsTask());
```

## Exception model

| Surface | Wraps in `LrcParseException`? |
|---|---|
| Strict-mode error diagnostic | ✅ — `FirstError` and `PartialResult` populated |
| Encoding could not be detected | ✅ — message-only |
| `IOException`, `FileNotFoundException`, `UnauthorizedAccessException` | ❌ — propagated raw |
| `OperationCanceledException` | ❌ — propagated raw |

`ParseFile` / `ParseFileAsync` annotate any `LrcParseException` they catch with
[`FilePath`](xref:ModernLrc.LrcParseException.FilePath) so you know which file failed.
