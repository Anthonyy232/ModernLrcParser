using System.Runtime.CompilerServices;

namespace ModernLrc.Internal;

/// <summary>Forward-only cursor over a <see cref="ReadOnlySpan{T}"/> of chars.
/// Tracks 1-based line/column for diagnostic emission. ref-struct so it lives on the stack.</summary>
internal ref struct LrcCursor
{
    private readonly ReadOnlySpan<char> _source;

    /// <summary>Current absolute position into the source.</summary>
    public int Position;

    /// <summary>1-based line number.</summary>
    public int Line;

    /// <summary>Position of the start of the current line (for column = Position - LineStart + 1).</summary>
    public int LineStart;

    /// <summary>Initialises the cursor at position 0, line 1.</summary>
    public LrcCursor(ReadOnlySpan<char> source)
    {
        _source = source;
        Position = 0;
        Line = 1;
        LineStart = 0;
    }

    /// <summary>True if the cursor has consumed every character.</summary>
    public readonly bool IsAtEnd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Position >= _source.Length;
    }

    /// <summary>Source length (constant).</summary>
    public readonly int Length => _source.Length;

    /// <summary>1-based column of the cursor.</summary>
    public readonly int Column => Position - LineStart + 1;

    /// <summary>Peek the current character without advancing. Returns <c>'\0'</c> at end.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char Peek() => Position < _source.Length ? _source[Position] : '\0';

    /// <summary>Peek <paramref name="offset"/> ahead. Returns <c>'\0'</c> when out of range.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char PeekAt(int offset)
    {
        int target = Position + offset;
        return target >= 0 && target < _source.Length ? _source[target] : '\0';
    }

    /// <summary>If the current character matches <paramref name="c"/>, consume it and return true.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Match(char c)
    {
        if (Position < _source.Length && _source[Position] == c)
        {
            Advance();
            return true;
        }
        return false;
    }

    /// <summary>Advance one character (handles line tracking).</summary>
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

    /// <summary>Read-only span over everything from the current position to end of source.</summary>
    public readonly ReadOnlySpan<char> RemainingSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _source[Position..];
    }

    /// <summary>Slice <paramref name="length"/> chars starting at the current position (does not advance).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<char> Slice(int length)
        => _source.Slice(Position, length);

    /// <summary>Slice from <paramref name="start"/> (absolute) to current position.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<char> SliceFrom(int start)
        => _source[start..Position];

    /// <summary>Find the next occurrence of <paramref name="terminator"/> from the current position;
    /// returns its absolute index or <c>-1</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int IndexOf(char terminator)
    {
        int i = _source[Position..].IndexOf(terminator);
        return i < 0 ? -1 : Position + i;
    }

    /// <summary>Find the next occurrence of <paramref name="terminator"/> within the current line.
    /// Returns its absolute index, or <c>-1</c> if a line terminator (LF or CR) is encountered first
    /// or the search reaches end of source. Used to bound bracket scanning so a stray <c>]</c> on a
    /// later line cannot be mis-attributed to a tag opened on an earlier line.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int IndexOfWithinLine(char terminator)
    {
        var slice = _source[Position..];
        int idx = slice.IndexOfAny(terminator, '\n', '\r');
        if (idx < 0) return -1;
        return slice[idx] == terminator ? Position + idx : -1;
    }

    /// <summary>Find the next line-terminator char (\n or \r) from the current position;
    /// returns its absolute index, or <see cref="Length"/> if none.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int IndexOfLineEnd()
    {
        int i = _source[Position..].IndexOfAny('\n', '\r');
        return i < 0 ? _source.Length : Position + i;
    }

    /// <summary>Skip to (but not past) the next LF/CR. Used to advance past a malformed line.</summary>
    public void SkipToLineEnd()
    {
        // SIMD-friendly scan. Line/column tracking can be skipped because by definition
        // we stop *before* the line terminator — no '\n' is consumed here.
        int i = _source[Position..].IndexOfAny('\n', '\r');
        Position = i < 0 ? _source.Length : Position + i;
    }

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
