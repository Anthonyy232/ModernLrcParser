# ModernLrc

An LRC (lyrics) parser and writer for .NET 10. It reads basic LRC, multi-timestamp
lines, Enhanced LRC word timing and Walaoke voice markers, reports malformed input as
diagnostics instead of throwing, and writes documents back out. Native AOT compatible,
trim-safe, and no dependencies outside the BCL.

```sh
dotnet add package ModernLrc
```

| To | Read |
|---|---|
| Parse, edit and write your first file | [Getting started](articles/getting-started.md) |
| Look up a diagnostic code or use strict mode | [Diagnostics reference](articles/diagnostics-reference.md) |
| Read files that aren't UTF-8 | [Encoding pipeline](articles/encoding-pipeline.md) |
| Show lyrics in time with playback | [Playback](articles/playback.md) |
| Change parse or write settings | [Options reference](articles/options.md) |
| Browse every public type and member | [API reference](xref:ModernLrc) |
