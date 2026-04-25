using System.Buffers;

namespace ModernLrc;

/// <summary>Entry-point for writing an <see cref="LrcDocument"/>. All overloads converge on the canonical
/// <see cref="Write(LrcDocument, LrcWriteOptions?)"/>.</summary>
/// <remarks>
/// <para>Sinks: <see cref="string"/>, <see cref="TextWriter"/>, <see cref="Stream"/>,
/// <see cref="IBufferWriter{T}"/> (chars or UTF-8 bytes), fixed <see cref="Span{T}"/>
/// (via the <c>TryWrite</c> family), and a path on disk
/// (<see cref="WriteFile(LrcDocument, string, LrcWriteOptions?)"/>, atomic via temp + rename).</para>
/// <para>The <see cref="IBufferWriter{T}"/> and UTF-8 stream paths are zero-allocation beyond
/// consumer-buffer growth.</para>
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
    /// <summary>Render to a string. Internally renders into an
    /// <see cref="System.Buffers.ArrayBufferWriter{T}"/> of chars then materializes the
    /// final <see cref="string"/>. The buffer is pre-sized via <see cref="EstimateSize"/>
    /// so large documents don't accumulate copy-on-grow waste.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="options">Write options. Defaults to <see cref="LrcWriteOptions.Default"/>.</param>
    /// <returns>The fully-rendered LRC text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    public static string Write(LrcDocument document, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= LrcWriteOptions.Default;
        var bw = new ArrayBufferWriter<char>(InitialBufferHint(document, options));
        Internal.LrcSpanRenderer.RenderToChars(document, bw, options);
        return new string(bw.WrittenSpan);
    }

    /// <summary>Render to a <see cref="TextWriter"/> via a single span write — no intermediate
    /// <see cref="string"/> allocation.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="writer">Destination writer.</param>
    /// <param name="options">Write options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="writer"/> is null.</exception>
    public static void Write(LrcDocument document, TextWriter writer, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(writer);
        options ??= LrcWriteOptions.Default;
        var bw = new ArrayBufferWriter<char>(InitialBufferHint(document, options));
        Internal.LrcSpanRenderer.RenderToChars(document, bw, options);
        writer.Write(bw.WrittenSpan);
    }

    /// <summary>Pick an initial char-/byte-buffer size for the staging
    /// <see cref="ArrayBufferWriter{T}"/> used by the string, TextWriter, TryWrite, Stream
    /// and async-Stream sinks. Bumps the caller's <see cref="LrcWriteOptions.InitialBufferSize"/>
    /// up to the document's <see cref="EstimateSize"/> when that's larger so the buffer is
    /// sized once instead of doubling-and-copying through 5+ growth steps on a large document.
    /// EstimateSize is char-count-accurate; for the byte sinks it's a close approximation
    /// since LRC content is overwhelmingly ASCII — bytes ≈ chars, with at most one growth
    /// for files containing CJK / multibyte content.</summary>
    private static int InitialBufferHint(LrcDocument document, LrcWriteOptions options)
        => Math.Max(options.InitialBufferSize, EstimateSize(document, options));

    /// <summary>Render to a <see cref="Stream"/> using <see cref="LrcWriteOptions.Encoding"/>
    /// (default UTF-8 with no BOM). When the encoding is UTF-8, the writer takes a fast path
    /// that renders directly into bytes — no intermediate string copy.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="stream">Destination stream; must be writable.</param>
    /// <param name="options">Write options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static void Write(LrcDocument document, Stream stream, LrcWriteOptions? options = null)
        => WriteToStream(document, stream, options);

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
        options ??= LrcWriteOptions.Default;
        Internal.LrcSpanRenderer.RenderToChars(document, writer, options);
    }

    /// <summary>Render directly into an <see cref="IBufferWriter{T}"/> of UTF-8 bytes. Zero allocations
    /// beyond consumer buffer growth.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="writer">Destination buffer writer.</param>
    /// <param name="options">Write options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="writer"/> is null.</exception>
    public static void Write(LrcDocument document, IBufferWriter<byte> writer, LrcWriteOptions? options = null)
        => WriteToBufferWriter(document, writer, options);

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
        ArgumentNullException.ThrowIfNull(document);
        options ??= LrcWriteOptions.Default;
        var staging = new ArrayBufferWriter<char>(InitialBufferHint(document, options));
        Internal.LrcSpanRenderer.RenderToChars(document, staging, options);
        if (staging.WrittenCount > destination.Length) { charsWritten = 0; return false; }
        staging.WrittenSpan.CopyTo(destination);
        charsWritten = staging.WrittenCount;
        return true;
    }

    /// <summary>Try-render into a fixed UTF-8 byte buffer.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="destination">Destination span.</param>
    /// <param name="bytesWritten">Number of bytes written on success; 0 on failure.</param>
    /// <param name="options">Write options.</param>
    /// <returns><c>false</c> when <paramref name="destination"/> is too small; <c>true</c> on success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    public static bool TryWrite(LrcDocument document, Span<byte> destination, out int bytesWritten, LrcWriteOptions? options = null)
        => TryWriteToBytes(document, destination, out bytesWritten, options);

    // WriteAsync is in LrcWriter.Async.cs.
    // EstimateSize, EstimateByteSize, ValidateForWrite are in LrcWriter.Validation.cs.
    // Stream, IBufferWriter<byte>, TryWrite<byte>, WriteFile are in LrcWriter.ByteOutput.cs.
}
