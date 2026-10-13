using System.Buffers;
using System.Globalization;

namespace ModernLrc.Internal;

/// <summary>Appends chars to an <see cref="IBufferWriter{T}"/> without intermediate strings.
/// Call <see cref="Commit"/> when done.</summary>
internal ref struct CharBufferWriter
{
    private const int MinLease = 256;

    private readonly IBufferWriter<char> _writer;
    private Span<char> _current;
    private int _written;

    public CharBufferWriter(IBufferWriter<char> writer)
    {
        _writer = writer;
        _current = writer.GetSpan(MinLease);
        _written = 0;
    }

    public void Append(char c) => Append([c]);

    public void Append(scoped ReadOnlySpan<char> text)
    {
        if (text.Length > _current.Length - _written) Lease(text.Length);
        text.CopyTo(_current[_written..]);
        _written += text.Length;
    }

    public void AppendInvariant<T>(T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
    {
        int charsWritten;
        while (!value.TryFormat(_current[_written..], out charsWritten, format, CultureInfo.InvariantCulture))
            Lease(_current.Length - _written + 1);
        _written += charsWritten;
    }

    /// <summary>Flush what has been written and lease a span of at least <paramref name="size"/> chars.</summary>
    private void Lease(int size)
    {
        Commit();
        _current = _writer.GetSpan(Math.Max(size, MinLease));
    }

    public void Commit()
    {
        _writer.Advance(_written);
        _written = 0;
    }
}
