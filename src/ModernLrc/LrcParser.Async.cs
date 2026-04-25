namespace ModernLrc;

public static partial class LrcParser
{
    /// <summary>Async parse from a <see cref="TextReader"/>.</summary>
    /// <param name="reader">Source reader; consumed to end.</param>
    /// <param name="options">Parser options.</param>
    /// <param name="cancellationToken">Honored before reading and respected by
    /// <see cref="TextReader.ReadToEndAsync(CancellationToken)"/>.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    /// <exception cref="LrcParseException">Strict mode encountered an Error-severity diagnostic.</exception>
    public static async ValueTask<LrcParseResult> ParseAsync(
        TextReader reader,
        LrcParseOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        cancellationToken.ThrowIfCancellationRequested();
        var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return Parse(text.AsSpan(), options);
    }

    /// <summary>Async parse from a <see cref="Stream"/>. Reads all bytes into memory then
    /// applies the encoding pipeline (BOM → caller override → UTF-8 → fallback).</summary>
    /// <param name="stream">Source stream; must be readable.</param>
    /// <param name="options">Parser options.</param>
    /// <param name="cancellationToken">Honored at start and during stream copy.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not readable.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    /// <exception cref="LrcParseException">Encoding could not be detected, or strict-mode error.</exception>
    /// <example>
    /// <code>
    /// using var fs = File.OpenRead("song.lrc");
    /// var result = await LrcParser.ParseAsync(fs, cancellationToken: ct);
    /// </code>
    /// </example>
    public static async ValueTask<LrcParseResult> ParseAsync(
        Stream stream,
        LrcParseOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(stream));
        cancellationToken.ThrowIfCancellationRequested();

        options ??= LrcParseOptions.Default;

        if (stream is MemoryStream ms && ms.TryGetBuffer(out var seg))
            return Parse(seg.AsSpan(), options);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, options.ReadBufferSize, cancellationToken).ConfigureAwait(false);
        return Parse(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), options);
    }

    /// <summary>Async parse from a file at <paramref name="path"/>. Annotates strict-mode
    /// failures with <see cref="LrcParseException.FilePath"/>; IO and cancellation exceptions
    /// propagate raw.</summary>
    /// <param name="path">File path. Must be non-empty.</param>
    /// <param name="options">Parser options.</param>
    /// <param name="cancellationToken">Honored before opening and during stream copy.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    /// <exception cref="LrcParseException">Encoding could not be detected, or strict-mode error.</exception>
    public static async ValueTask<LrcParseResult> ParseFileAsync(
        string path,
        LrcParseOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));
        cancellationToken.ThrowIfCancellationRequested();
        options ??= LrcParseOptions.Default;
        try
        {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                options.ReadBufferSize, FileOptions.Asynchronous);
            await using (fs.ConfigureAwait(false))
            return await ParseAsync(fs, options, cancellationToken).ConfigureAwait(false);
        }
        catch (LrcParseException ex) when (ex.FilePath is null)
        {
            // Re-wrap with file path filled in (catches both encoding and content failures).
            throw new LrcParseException(ex.Message, ex.PartialResult, ex.FirstError, path);
        }
    }
}
