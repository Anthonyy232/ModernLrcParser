using System.Buffers;

namespace ModernLrc;

public static partial class LrcWriter
{
    // Stream overload
    private static void WriteToStream(LrcDocument document, Stream stream, LrcWriteOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));
        options ??= LrcWriteOptions.Default;

        if (options.EmitByteOrderMark)
        {
            var preamble = options.Encoding.GetPreamble();
            if (preamble.Length > 0) stream.Write(preamble);
        }

        // UTF-8 fast path: render straight into bytes via the span renderer; skip the
        // intermediate string + GetBytes copy.
        if (options.Encoding is System.Text.UTF8Encoding)
        {
            var staging = new ArrayBufferWriter<byte>(InitialBufferHint(document, options));
            Internal.LrcSpanRenderer.RenderToUtf8(document, staging, options);
            stream.Write(staging.WrittenSpan);
            return;
        }

        // Non-UTF-8 encoding (rare): fall back to the encode-from-string path.
        var content = Write(document, options);
        var bytes = options.Encoding.GetBytes(content);
        stream.Write(bytes);
    }

    // IBufferWriter<byte> overload
    private static void WriteToBufferWriter(LrcDocument document, IBufferWriter<byte> writer, LrcWriteOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(writer);
        options ??= LrcWriteOptions.Default;

        // BOM must be written before content.
        if (options.EmitByteOrderMark)
        {
            var preamble = options.Encoding.GetPreamble();
            if (preamble.Length > 0)
            {
                var pSpan = writer.GetSpan(preamble.Length);
                preamble.CopyTo(pSpan);
                writer.Advance(preamble.Length);
            }
        }

        if (options.Encoding is System.Text.UTF8Encoding)
        {
            Internal.LrcSpanRenderer.RenderToUtf8(document, writer, options);
            return;
        }

        var content = Write(document, options);
        int byteCount = options.Encoding.GetByteCount(content);
        var span = writer.GetSpan(byteCount);
        int bytesWritten = options.Encoding.GetBytes(content.AsSpan(), span);
        writer.Advance(bytesWritten);
    }

    // TryWrite byte overload
    private static bool TryWriteToBytes(LrcDocument document, Span<byte> destination, out int bytesWritten, LrcWriteOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= LrcWriteOptions.Default;

        if (options.Encoding is not System.Text.UTF8Encoding)
            return TryWriteToEncodedBytes(document, destination, out bytesWritten, options);

        // Render to a staging buffer to get the exact byte count, then copy if it fits.
        // The staging buffer is the only allocation here.
        var staging = new ArrayBufferWriter<byte>(InitialBufferHint(document, options));

        // BOM goes into the staging buffer first so the size check is inclusive.
        if (options.EmitByteOrderMark)
        {
            var preamble = options.Encoding.GetPreamble();
            if (preamble.Length > 0)
                staging.Write(preamble);
        }

        Internal.LrcSpanRenderer.RenderToUtf8(document, staging, options);

        if (staging.WrittenCount > destination.Length) { bytesWritten = 0; return false; }
        staging.WrittenSpan.CopyTo(destination);
        bytesWritten = staging.WrittenCount;
        return true;
    }

    private static bool TryWriteToEncodedBytes(LrcDocument document, Span<byte> destination, out int bytesWritten, LrcWriteOptions options)
    {
        byte[] preamble = options.EmitByteOrderMark ? options.Encoding.GetPreamble() : [];
        var content = Write(document, options);
        int bodyByteCount = options.Encoding.GetByteCount(content);
        long totalByteCount = (long)preamble.Length + bodyByteCount;

        if (totalByteCount > destination.Length) { bytesWritten = 0; return false; }

        preamble.CopyTo(destination);
        int bodyBytesWritten = options.Encoding.GetBytes(content.AsSpan(), destination[preamble.Length..]);
        bytesWritten = preamble.Length + bodyBytesWritten;
        return true;
    }

    /// <summary>Atomic file write: writes to a temp file in the destination directory then moves
    /// it into place with overwrite. Uses <see cref="FileShare.None"/> on the temp file. The temp
    /// file is cleaned up only on failure.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="path">Destination file path. Must be non-empty.</param>
    /// <param name="options">Write options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="IOException">A filesystem error occurred. The temp file is cleaned up best-effort.</exception>
    /// <exception cref="UnauthorizedAccessException">The destination cannot be written.</exception>
    /// <example>
    /// <code>
    /// LrcWriter.WriteFile(doc, "song.lrc");
    /// // On success: "song.lrc" exists, no temp file remains.
    /// // On failure: any temp file in the destination dir is best-effort cleaned up.
    /// </code>
    /// </example>
    public static void WriteFile(LrcDocument document, string path, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
        options ??= LrcWriteOptions.Default;

        var dir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Path.GetTempPath();
        var tmp = Path.Combine(dir, Path.GetRandomFileName());
        bool moved = false;
        try
        {
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                WriteToStream(document, fs, options);
            }
            File.Move(tmp, path, overwrite: true);
            moved = true;
        }
        finally
        {
            if (!moved && File.Exists(tmp))
            {
                try { File.Delete(tmp); }
                catch (IOException) { /* best-effort — ignore if tmp cannot be deleted */ }
                catch (UnauthorizedAccessException) { /* best-effort */ }
            }
        }
    }
}
