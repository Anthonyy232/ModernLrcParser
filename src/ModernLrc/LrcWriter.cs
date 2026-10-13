using System.Buffers;

namespace ModernLrc;

/// <summary>Entry-point for writing an <see cref="LrcDocument"/>. All overloads converge on a
/// single char renderer; byte sinks encode its output with <see cref="LrcWriteOptions.Encoding"/>.</summary>
/// <remarks>
/// <para>Sinks: <see cref="string"/>, <see cref="TextWriter"/>, <see cref="Stream"/>,
/// <see cref="IBufferWriter{T}"/> (chars or encoded bytes), fixed <see cref="Span{T}"/>
/// (via the <c>TryWrite</c> family), and a path on disk
/// (<see cref="WriteFile(LrcDocument, string, LrcWriteOptions?)"/>, atomic via temp + rename).</para>
/// <para>The <see cref="IBufferWriter{T}"/> char sink is zero-allocation beyond consumer-buffer growth.</para>
/// </remarks>
/// <example>
/// <code>
/// using ModernLrc;
/// using ModernLrc.Model;
///
/// var doc = new LrcDocumentBuilder()
///     .WithTitle("My Song")
///     .AddLine("00:12.00", "first line")
///     .AddLine("00:14.20", "second line", LrcVoice.Female)
///     .Build();
///
/// string lrc = LrcWriter.Write(doc);
/// // [ti:My Song]
/// //
/// // [00:12.00]first line
/// // [00:14.20]F: second line
/// </code>
/// </example>
public static partial class LrcWriter
{
    /// <summary>Render to a string.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="options">Write options. Defaults to <see cref="LrcWriteOptions.Default"/>.</param>
    /// <returns>The fully-rendered LRC text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    public static string Write(LrcDocument document, LrcWriteOptions? options = null)
    {
        using var chars = RenderChars(document, options);
        return new(chars.WrittenSpan);
    }

    /// <summary>Render to a <see cref="TextWriter"/> via a single span write — no intermediate
    /// <see cref="string"/> allocation.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="writer">Destination writer.</param>
    /// <param name="options">Write options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="writer"/> is null.</exception>
    public static void Write(LrcDocument document, TextWriter writer, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        using var chars = RenderChars(document, options);
        writer.Write(chars.WrittenSpan);
    }

    /// <summary>Render to a <see cref="Stream"/> using <see cref="LrcWriteOptions.Encoding"/>
    /// (default UTF-8, no BOM unless <see cref="LrcWriteOptions.EmitByteOrderMark"/> is set).</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="stream">Destination stream; must be writable.</param>
    /// <param name="options">Write options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static void Write(LrcDocument document, Stream stream, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));
        stream.Write(Encode(document, options));
    }

    /// <summary>Render directly into an <see cref="IBufferWriter{T}"/> of chars. Zero allocations
    /// beyond consumer buffer growth.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="writer">Destination buffer writer.</param>
    /// <param name="options">Write options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="writer"/> is null.</exception>
    /// <example>
    /// <code>
    /// var bw = new System.Buffers.ArrayBufferWriter&lt;char&gt;();
    /// LrcWriter.Write(doc, bw);
    /// var span = bw.WrittenSpan;
    /// </code>
    /// </example>
    public static void Write(LrcDocument document, IBufferWriter<char> writer, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(writer);
        Internal.LrcSpanRenderer.Render(document, writer, options ?? LrcWriteOptions.Default);
    }

    /// <summary>Render into an <see cref="IBufferWriter{T}"/> of bytes encoded with
    /// <see cref="LrcWriteOptions.Encoding"/>.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="writer">Destination buffer writer.</param>
    /// <param name="options">Write options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="writer"/> is null.</exception>
    public static void Write(LrcDocument document, IBufferWriter<byte> writer, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        options ??= LrcWriteOptions.Default;
        using var chars = RenderChars(document, options);
        if (options.EmitByteOrderMark) writer.Write(options.Encoding.Preamble);
        System.Text.EncodingExtensions.GetBytes(options.Encoding, chars.WrittenSpan, writer);
    }

    /// <summary>Try-render into a fixed char buffer.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="destination">Destination span.</param>
    /// <param name="charsWritten">Number of chars written on success; 0 on failure.</param>
    /// <param name="options">Write options.</param>
    /// <returns><c>false</c> when <paramref name="destination"/> is too small; <c>true</c> on success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    /// <remarks>The implementation renders to a staging buffer first to know the exact size,
    /// then copies into <paramref name="destination"/>. For zero-allocation rendering use the
    /// <see cref="IBufferWriter{T}"/> overload instead.</remarks>
    public static bool TryWrite(LrcDocument document, Span<char> destination, out int charsWritten, LrcWriteOptions? options = null)
    {
        using var chars = RenderChars(document, options);
        return TryCopy(chars.WrittenSpan, destination, out charsWritten);
    }

    /// <summary>Try-render into a fixed byte buffer using <see cref="LrcWriteOptions.Encoding"/>.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="destination">Destination span.</param>
    /// <param name="bytesWritten">Number of bytes written on success; 0 on failure.</param>
    /// <param name="options">Write options.</param>
    /// <returns><c>false</c> when <paramref name="destination"/> is too small; <c>true</c> on success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    public static bool TryWrite(LrcDocument document, Span<byte> destination, out int bytesWritten, LrcWriteOptions? options = null)
        => TryCopy(Encode(document, options), destination, out bytesWritten);

    private static Internal.PooledCharBuffer RenderChars(LrcDocument document, LrcWriteOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        var buffer = new Internal.PooledCharBuffer();
        Internal.LrcSpanRenderer.Render(document, buffer, options ?? LrcWriteOptions.Default);
        return buffer;
    }

    /// <summary>Render and encode, including the preamble when <see cref="LrcWriteOptions.EmitByteOrderMark"/> is set.</summary>
    private static byte[] Encode(LrcDocument document, LrcWriteOptions? options)
    {
        options ??= LrcWriteOptions.Default;
        using var rendered = RenderChars(document, options);
        var chars = rendered.WrittenSpan;
        var preamble = options.EmitByteOrderMark ? options.Encoding.Preamble : default;
        var bytes = new byte[preamble.Length + options.Encoding.GetByteCount(chars)];
        preamble.CopyTo(bytes);
        options.Encoding.GetBytes(chars, bytes.AsSpan(preamble.Length));
        return bytes;
    }

    private static bool TryCopy<T>(ReadOnlySpan<T> source, Span<T> destination, out int written)
    {
        if (!source.TryCopyTo(destination)) { written = 0; return false; }
        written = source.Length;
        return true;
    }

    /// <summary>Atomic file write: writes to a temp file in the destination directory then moves
    /// it into place with overwrite. The temp file is cleaned up only on failure.</summary>
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
        var tmp = TempPathFor(path);
        var bytes = Encode(document, options);
        try
        {
            File.WriteAllBytes(tmp, bytes);
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            TryDelete(tmp);
            throw;
        }
    }

    private static string TempPathFor(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
        var dir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Path.GetTempPath();
        return Path.Combine(dir, Path.GetRandomFileName());
    }

    private static void TryDelete(string tmp)
    {
        try { File.Delete(tmp); }
        catch (IOException) { /* best-effort */ }
        catch (UnauthorizedAccessException) { /* best-effort */ }
    }
}
