namespace ModernLrc;

public static partial class LrcWriter
{
    /// <summary>Async render to a <see cref="TextWriter"/>.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="writer">Destination writer.</param>
    /// <param name="options">Write options.</param>
    /// <param name="cancellationToken">Honored before the synchronous render and during the
    /// asynchronous write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="writer"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    public static async ValueTask WriteAsync(LrcDocument document, TextWriter writer, LrcWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(writer);
        cancellationToken.ThrowIfCancellationRequested();
        await writer.WriteAsync(Write(document, options).AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Async render to a <see cref="Stream"/> using <see cref="LrcWriteOptions.Encoding"/>.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="stream">Destination stream; must be writable.</param>
    /// <param name="options">Write options.</param>
    /// <param name="cancellationToken">Honored before render and during async write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    /// <example>
    /// <code>
    /// using var fs = File.Create("out.lrc");
    /// await LrcWriter.WriteAsync(doc, fs, cancellationToken: ct);
    /// </code>
    /// </example>
    public static async ValueTask WriteAsync(LrcDocument document, Stream stream, LrcWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));
        cancellationToken.ThrowIfCancellationRequested();
        await stream.WriteAsync(Encode(document, options), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Async atomic file write: writes to a temp file in the destination directory and
    /// then moves it into place with overwrite. The temp file is cleaned up only on failure.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="path">Destination file path. Must be non-empty.</param>
    /// <param name="options">Write options.</param>
    /// <param name="cancellationToken">Honored before opening and during writes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="IOException">A filesystem error occurred. The temp file is cleaned up best-effort.</exception>
    /// <exception cref="UnauthorizedAccessException">The destination cannot be written.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    public static async ValueTask WriteFileAsync(LrcDocument document, string path, LrcWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        var tmp = TempPathFor(path);
        var bytes = Encode(document, options);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await File.WriteAllBytesAsync(tmp, bytes, cancellationToken).ConfigureAwait(false);
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            TryDelete(tmp);
            throw;
        }
    }
}
