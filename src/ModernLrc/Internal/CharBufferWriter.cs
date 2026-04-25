using System.Buffers;
using System.Globalization;

namespace ModernLrc.Internal;

/// <summary>
/// A <c>ref struct</c> helper that accumulates characters into an <see cref="IBufferWriter{T}"/>
/// of <see cref="char"/> without allocating intermediate strings.
/// Call <see cref="Commit"/> when done to flush any remaining buffered characters.
/// </summary>
internal ref struct CharBufferWriter
{
    private readonly IBufferWriter<char> _writer;
    private Span<char> _current;
    private int _written;

    public CharBufferWriter(IBufferWriter<char> writer, int initialHint = 256)
    {
        _writer = writer;
        _current = writer.GetSpan(initialHint);
        _written = 0;
    }

    public void Append(char c)
    {
        if (_written >= _current.Length) RotateBuffer(1);
        _current[_written++] = c;
    }

    public void Append(ReadOnlySpan<char> text)
    {
        while (true)
        {
            int remaining = _current.Length - _written;
            if (text.Length <= remaining)
            {
                text.CopyTo(_current[_written..]);
                _written += text.Length;
                return;
            }
            // Copy what fits, flush, lease a new span, then loop with the remainder.
            text[..remaining].CopyTo(_current[_written..]);
            _written += remaining;
            text = text[remaining..];
            RotateBuffer(text.Length);
        }
    }

    public void Append(string text) => Append(text.AsSpan());

    /// <summary>
    /// Format <paramref name="value"/> directly into the remaining space using
    /// <see cref="CultureInfo.InvariantCulture"/>; rotates the buffer and retries if the
    /// space is insufficient.
    /// </summary>
    public void AppendInvariant<T>(T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
    {
        while (true)
        {
            if (value.TryFormat(_current[_written..], out int charsWritten, format, CultureInfo.InvariantCulture))
            {
                _written += charsWritten;
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

    /// <summary>Flush any remaining buffered characters to the underlying writer.</summary>
    public void Commit()
    {
        if (_written > 0)
        {
            _writer.Advance(_written);
            _written = 0;
        }
    }
}
