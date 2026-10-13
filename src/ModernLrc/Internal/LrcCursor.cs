using System.Runtime.CompilerServices;

namespace ModernLrc.Internal;

/// <summary>Forward-only cursor over a span of chars. Tracks 1-based line/column for diagnostics.</summary>
internal ref struct LrcCursor
{
    private readonly ReadOnlySpan<char> _source;

    public int Position;

    public int Line;

    /// <summary>Position of the first char of the current line; <see cref="Column"/> is relative to it.</summary>
    public int LineStart;

    public LrcCursor(ReadOnlySpan<char> source)
    {
        _source = source;
        Position = 0;
        Line = 1;
        LineStart = 0;
    }

    public readonly bool IsAtEnd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Position >= _source.Length;
    }

    public readonly int Length => _source.Length;

    public readonly int Column => Position - LineStart + 1;

    /// <summary>Returns <c>'\0'</c> at end.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char Peek() => Position < _source.Length ? _source[Position] : '\0';

    /// <summary>Returns <c>'\0'</c> when out of range.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char PeekAt(int offset)
    {
        int target = Position + offset;
        return target >= 0 && target < _source.Length ? _source[target] : '\0';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance()
    {
        if (Position < _source.Length)
        {
            char c = _source[Position++];
            if (c == '\n')
            {
                Line++;
                LineStart = Position;
            }
        }
    }

    public readonly ReadOnlySpan<char> RemainingSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _source[Position..];
    }

    /// <summary>Slice <paramref name="length"/> chars from the current position without advancing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<char> Slice(int length)
        => _source.Slice(Position, length);

    /// <summary>Absolute index of the next <paramref name="terminator"/> on the current line,
    /// or <c>-1</c> if a line ending (LF or CR) or the end of input comes first.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int IndexOfWithinLine(char terminator)
    {
        var slice = _source[Position..];
        int idx = slice.IndexOfAny(terminator, '\n', '\r');
        if (idx < 0) return -1;
        return slice[idx] == terminator ? Position + idx : -1;
    }

    /// <summary>Absolute index of the next LF/CR, or <see cref="Length"/> if none.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int IndexOfLineEnd()
    {
        int i = _source[Position..].IndexOfAny('\n', '\r');
        return i < 0 ? _source.Length : Position + i;
    }

    /// <summary>Skip to (but not past) the next LF/CR. No '\n' is consumed, so line tracking is unaffected.</summary>
    public void SkipToLineEnd() => Position = IndexOfLineEnd();

    /// <summary>Consume the line terminator if present (LF, CR, or CRLF).</summary>
    public void ConsumeLineTerminator()
    {
        if (Position >= _source.Length) return;
        if (_source[Position] == '\r')
        {
            Advance();
            if (Position < _source.Length && _source[Position] == '\n') Advance();
        }
        else if (_source[Position] == '\n')
        {
            Advance();
        }
    }
}
