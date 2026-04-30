using ModernLrc.Internal;

namespace ModernLrc;

public static partial class LrcParser
{
    /// <summary>Parse a span of bytes through the encoding pipeline:
    /// BOM (UTF-8 / UTF-16 LE / UTF-16 BE) → caller-supplied <see cref="LrcParseOptions.Encoding"/>
    /// → UTF-8 validation → <see cref="LrcParseOptions.FallbackEncoding"/>.</summary>
    /// <param name="input">Source bytes.</param>
    /// <param name="options">Parser options.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="LrcParseException">Encoding could not be detected (no BOM, invalid UTF-8,
    /// and <see cref="LrcParseOptions.FallbackEncoding"/> is null), or strict-mode error.</exception>
    public static LrcParseResult Parse(ReadOnlySpan<byte> input, LrcParseOptions? options = null)
    {
        options ??= LrcParseOptions.Default;
        var emitter = new LrcDiagnosticEmitter(options);
        var decoded = EncodingDetector.Decode(input, options, emitter);
        var scanner = new LrcScanner(decoded.AsSpan(), options, emitter);
        var result = scanner.Run();
        if (emitter.IsStrict && emitter.FirstError is not null)
            throw new LrcParseException("Strict-mode parse failed.", result, emitter.FirstError, filePath: null);
        return result;
    }

    /// <summary>Parse a byte array (encoding pipeline applied — see the
    /// <see cref="Parse(ReadOnlySpan{byte}, LrcParseOptions?)"/> overload for the order of operations).</summary>
    /// <param name="input">Source bytes.</param>
    /// <param name="options">Parser options.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
    /// <exception cref="LrcParseException">Encoding could not be detected, or strict-mode error.</exception>
    public static LrcParseResult Parse(byte[] input, LrcParseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Parse((ReadOnlySpan<byte>)input, options);
    }

    /// <summary>Parse a stream synchronously. Reads all bytes into memory first; for large
    /// inputs prefer <see cref="ParseAsync(Stream, LrcParseOptions?, CancellationToken)"/>.</summary>
    /// <param name="stream">Source stream; must be readable.</param>
    /// <param name="options">Parser options.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not readable.</exception>
    /// <exception cref="LrcParseException">Encoding could not be detected, or strict-mode error.</exception>
    public static LrcParseResult Parse(Stream stream, LrcParseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(stream));

        options ??= LrcParseOptions.Default;

        if (stream is MemoryStream ms && ms.TryGetBuffer(out var seg))
            return ParseMemoryStreamBuffer(ms, seg, options);

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer, options.ReadBufferSize);
        return Parse(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), options);
    }

    private static LrcParseResult ParseMemoryStreamBuffer(MemoryStream stream, ArraySegment<byte> segment, LrcParseOptions options)
    {
        if (stream.Position >= stream.Length)
            return Parse(ReadOnlySpan<byte>.Empty, options);

        int offset = checked((int)stream.Position);
        int count = checked((int)(stream.Length - stream.Position));
        var remaining = segment.AsSpan(offset, count);
        stream.Position = stream.Length;
        return Parse(remaining, options);
    }

    /// <summary>Parse a file at <paramref name="path"/> synchronously. Annotates strict-mode
    /// failures with <see cref="LrcParseException.FilePath"/>; IO exceptions propagate raw.</summary>
    /// <param name="path">File path. Must be non-empty.</param>
    /// <param name="options">Parser options.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    /// <exception cref="LrcParseException">Encoding could not be detected, or strict-mode error.</exception>
    /// <example>
    /// <code>
    /// var result = LrcParser.ParseFile("song.lrc");
    /// if (result.HasErrors)
    ///     foreach (var d in result.Diagnostics.Where(d => d.Severity == LrcDiagnosticSeverity.Error))
    ///         Console.WriteLine(d.Message);
    /// </code>
    /// </example>
    public static LrcParseResult ParseFile(string path, LrcParseOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));
        options ??= LrcParseOptions.Default;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, options.ReadBufferSize);
            return Parse(fs, options);
        }
        catch (LrcParseException ex) when (ex.FilePath is null)
        {
            throw new LrcParseException(ex.Message, ex.PartialResult, ex.FirstError, path);
        }
    }
}
