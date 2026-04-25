using System.Buffers;
using System.Globalization;
using System.Text;

namespace ModernLrc.Internal;

/// <summary>
/// A <c>ref struct</c> helper that accumulates UTF-8 bytes into an <see cref="IBufferWriter{T}"/>
/// of <see cref="byte"/> without allocating intermediate strings.
/// Call <see cref="Commit"/> when done to flush any remaining buffered bytes.
/// </summary>
internal ref struct Utf8BufferWriter
{
    private readonly IBufferWriter<byte> _writer;
    private Span<byte> _current;
    private int _written;

    public Utf8BufferWriter(IBufferWriter<byte> writer, int initialHint = 256)
    {
        _writer = writer;
        _current = writer.GetSpan(initialHint);
        _written = 0;
    }

    public void Append(byte b)
    {
        if (_written >= _current.Length) RotateBuffer(1);
        _current[_written++] = b;
    }

    public void Append(ReadOnlySpan<byte> bytes)
    {
        while (true)
        {
            int remaining = _current.Length - _written;
            if (bytes.Length <= remaining)
            {
                bytes.CopyTo(_current[_written..]);
                _written += bytes.Length;
                return;
            }
            // Copy what fits, flush, lease a new span, then loop with the remainder.
            bytes[..remaining].CopyTo(_current[_written..]);
            _written += remaining;
            bytes = bytes[remaining..];
            RotateBuffer(bytes.Length);
        }
    }

    /// <summary>
    /// Encode <paramref name="text"/> as UTF-8 and append the bytes.
    /// For valid Unicode input (no unmappable chars) this is allocation-free.
    /// </summary>
    public void AppendText(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return;

        // Compute max bytes needed and ensure the buffer has room.
        int maxBytes = Encoding.UTF8.GetMaxByteCount(text.Length);
        while (true)
        {
            int remaining = _current.Length - _written;
            if (remaining >= maxBytes)
            {
                int written = Encoding.UTF8.GetBytes(text, _current[_written..]);
                _written += written;
                return;
            }
            // Not enough room; flush and get a bigger span.
            RotateBuffer(maxBytes);
        }
    }

    public void AppendText(string text) => AppendText(text.AsSpan());

    /// <summary>
    /// Format <paramref name="value"/> directly into the remaining space as UTF-8 using
    /// <see cref="CultureInfo.InvariantCulture"/>; rotates the buffer and retries if the
    /// space is insufficient.
    /// </summary>
    public void AppendInvariant<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        while (true)
        {
            if (value.TryFormat(_current[_written..], out int bytesWritten, format, CultureInfo.InvariantCulture))
            {
                _written += bytesWritten;
                return;
            }
            RotateBuffer(64);
        }
    }

    private void RotateBuffer(int hintForNext)
    {
        if (_written > 0)
        {
            _writer.Advance(_written);
            _written = 0;
        }
        _current = _writer.GetSpan(Math.Max(hintForNext, 256));
    }

    /// <summary>Flush any remaining buffered bytes to the underlying writer.</summary>
    public void Commit()
    {
        if (_written > 0)
        {
            _writer.Advance(_written);
            _written = 0;
        }
    }
}
