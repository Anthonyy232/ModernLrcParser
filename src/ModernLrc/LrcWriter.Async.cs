namespace ModernLrc;

public static partial class LrcWriter
{
    /// <summary>Async render to a <see cref="TextWriter"/>.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="writer">Destination writer.</param>
    /// <param name="options">Write options.</param>
    /// <param name="cancellationToken">Honored before the synchronous render and during the
    /// asynchronous flush.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="writer"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    public static async ValueTask WriteAsync(LrcDocument document, TextWriter writer, LrcWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(writer);
        cancellationToken.ThrowIfCancellationRequested();
        var content = Write(document, options);
        await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Async render to a <see cref="Stream"/>. When the encoding is UTF-8, the writer
    /// takes a fast path that renders directly into bytes — no intermediate string copy.</summary>
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
        options ??= LrcWriteOptions.Default;

        if (options.EmitByteOrderMark)
        {
            var preamble = options.Encoding.GetPreamble();
            if (preamble.Length > 0)
                await stream.WriteAsync(preamble.AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        // UTF-8 fast path: render directly into bytes; skips the intermediate string + GetBytes.
        // Encoding.UTF8 returns a UTF8Encoding singleton, so the type check covers both the
        // singleton and any caller-supplied UTF8Encoding instance (e.g. with custom error fallback).
        if (options.Encoding is System.Text.UTF8Encoding)
        {
            var staging = new System.Buffers.ArrayBufferWriter<byte>(InitialBufferHint(document, options));
            Internal.LrcSpanRenderer.RenderToUtf8(document, staging, options);
            await stream.WriteAsync(staging.WrittenMemory, cancellationToken).ConfigureAwait(false);
            return;
        }

        var content = Write(document, options);
        var bytes = options.Encoding.GetBytes(content);
        await stream.WriteAsync(bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
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
        ArgumentNullException.ThrowIfNull(document);
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
        cancellationToken.ThrowIfCancellationRequested();
        options ??= LrcWriteOptions.Default;

        var dir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Path.GetTempPath();
        var tmp = Path.Combine(dir, Path.GetRandomFileName());
        bool moved = false;
        try
        {
            var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (fs.ConfigureAwait(false))
            {
                await WriteAsync(document, fs, options, cancellationToken).ConfigureAwait(false);
            }
            File.Move(tmp, path, overwrite: true);
            moved = true;
        }
        finally
        {
            if (!moved && File.Exists(tmp))
            {
                try { File.Delete(tmp); }
                catch (IOException) { /* best-effort */ }
                catch (UnauthorizedAccessException) { /* best-effort */ }
            }
        }
    }
}
