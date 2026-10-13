using System.Buffers;

namespace ModernLrc.Internal;

/// <summary>Staging buffer for sinks that need the whole render before writing. Rents from
/// <see cref="ArrayPool{T}.Shared"/> so growth leaves no garbage behind.</summary>
internal sealed class PooledCharBuffer : IBufferWriter<char>, IDisposable
{
    private char[] _buffer = ArrayPool<char>.Shared.Rent(4096);
    private int _written;

    public ReadOnlySpan<char> WrittenSpan => _buffer.AsSpan(0, _written);

    public void Advance(int count) => _written += count;

    public Memory<char> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_written);
    }

    public Span<char> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_written);
    }

    private void EnsureCapacity(int sizeHint)
    {
        int needed = _written + Math.Max(sizeHint, 1);
        if (needed <= _buffer.Length) return;
        var grown = ArrayPool<char>.Shared.Rent(Math.Max(needed, _buffer.Length * 2));
        WrittenSpan.CopyTo(grown);
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = grown;
    }

    public void Dispose()
    {
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = [];
    }
}
