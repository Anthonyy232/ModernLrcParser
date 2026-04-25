using System.Diagnostics.CodeAnalysis;

namespace ModernLrc.Model;

/// <summary>
/// Non-negative monotonic time offset within an LRC document, stored as 100ns ticks
/// (matching <see cref="TimeSpan.Ticks"/>). LRC syntax has no negative form, so every
/// factory enforces <c>&#x2265; 0</c>; <c>LrcDocumentExtensions.GetEffectiveTime</c>
/// returns <see cref="TimeSpan"/> (signed) for offset application.
/// </summary>
/// <remarks>Implements <see cref="IParsable{TSelf}"/>, <see cref="ISpanParsable{TSelf}"/>,
/// <see cref="IFormattable"/>, <see cref="ISpanFormattable"/>, and
/// <see cref="IUtf8SpanFormattable"/>. Parse accepts the canonical <c>mm:ss.xx</c> as well as
/// tolerant variants (3-digit fraction, colon fraction, hours notation, comma decimal).
/// Format codes: <c>"G"</c> (default, <c>mm:ss.xx</c>), <c>"F"</c> (<c>mm:ss.xxx</c>),
/// <c>"S"</c> (<c>mm:ss</c>), <c>"B"</c> (<c>[mm:ss.xx]</c>), <c>"W"</c> (<c>&lt;mm:ss.xx&gt;</c>).</remarks>
/// <example>
/// <code>
/// var ts = LrcTimestamp.FromMilliseconds(83_450);
/// Console.WriteLine(ts);                                  // "01:23.45"
/// Console.WriteLine(ts.ToString("F", null));              // "01:23.450"
/// Console.WriteLine(ts.ToString("B", null));              // "[01:23.45]"
///
/// LrcTimestamp parsed = LrcTimestamp.Parse("01:23.45");
/// LrcTimestamp shifted = parsed + TimeSpan.FromSeconds(1);
/// </code>
/// </example>
public readonly partial struct LrcTimestamp :
    IEquatable<LrcTimestamp>, IComparable<LrcTimestamp>,
    IFormattable, ISpanFormattable, IUtf8SpanFormattable,
    IParsable<LrcTimestamp>, ISpanParsable<LrcTimestamp>
{
    private readonly long _ticks;

    /// <summary>The zero timestamp.</summary>
    public static LrcTimestamp Zero { get; }

    private LrcTimestamp(long ticks) => _ticks = ticks;

    /// <summary>100ns units. Always <c>&gt;= 0</c>.</summary>
    public long Ticks => _ticks;

    /// <summary>Whole milliseconds (truncated).</summary>
    public int TotalMilliseconds => (int)(_ticks / TimeSpan.TicksPerMillisecond);

    /// <summary>Total seconds as floating-point.</summary>
    public double TotalSeconds => (double)_ticks / TimeSpan.TicksPerSecond;

    /// <summary>Convert to <see cref="TimeSpan"/>; always non-negative.</summary>
    public TimeSpan ToTimeSpan() => new(_ticks);

    /// <summary>Construct from raw 100ns ticks. Throws on negative.</summary>
    public static LrcTimestamp FromTicks(long ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        return new LrcTimestamp(ticks);
    }

    /// <summary>Construct from milliseconds. Throws on negative.</summary>
    public static LrcTimestamp FromMilliseconds(long ms)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ms);
        return new LrcTimestamp(ms * TimeSpan.TicksPerMillisecond);
    }

    /// <summary>Construct from a <see cref="TimeSpan"/>. Throws if the input is negative.</summary>
    public static LrcTimestamp FromTimeSpan(TimeSpan ts)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ts.Ticks, nameof(ts));
        return new LrcTimestamp(ts.Ticks);
    }

    /// <summary>Implicit lift to <see cref="TimeSpan"/>.</summary>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
        Justification = "Implicit lift to TimeSpan is the canonical interop; ToTimeSpan() serves as the named alternate.")]
    public static implicit operator TimeSpan(LrcTimestamp t) => t.ToTimeSpan();

    /// <summary>Add a <see cref="TimeSpan"/>. Throws <see cref="OverflowException"/> if the result would be negative.</summary>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
        Justification = "Arithmetic on LrcTimestamp is intentionally operator-only; named helpers would pollute the API surface for a value type used in hot paths.")]
    public static LrcTimestamp operator +(LrcTimestamp left, TimeSpan right)
    {
        long sum = checked(left._ticks + right.Ticks);
        if (sum < 0)
            throw new OverflowException("Resulting LrcTimestamp would be negative.");
        return new LrcTimestamp(sum);
    }

    /// <summary>Subtract a <see cref="TimeSpan"/>. Throws <see cref="OverflowException"/> if the result would be negative.</summary>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
        Justification = "Arithmetic on LrcTimestamp is intentionally operator-only; named helpers would pollute the API surface for a value type used in hot paths.")]
    public static LrcTimestamp operator -(LrcTimestamp left, TimeSpan right)
    {
        long sum = checked(left._ticks - right.Ticks);
        if (sum < 0)
            throw new OverflowException("Resulting LrcTimestamp would be negative.");
        return new LrcTimestamp(sum);
    }

    /// <summary>Subtract two timestamps producing a (signed) <see cref="TimeSpan"/>.</summary>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
        Justification = "Arithmetic on LrcTimestamp is intentionally operator-only; named helpers would pollute the API surface for a value type used in hot paths.")]
    public static TimeSpan operator -(LrcTimestamp left, LrcTimestamp right)
        => new(left._ticks - right._ticks);

    /// <summary>Equality.</summary>
    public static bool operator ==(LrcTimestamp left, LrcTimestamp right) => left._ticks == right._ticks;

    /// <summary>Inequality.</summary>
    public static bool operator !=(LrcTimestamp left, LrcTimestamp right) => left._ticks != right._ticks;

    /// <summary>Less than.</summary>
    public static bool operator <(LrcTimestamp left, LrcTimestamp right) => left._ticks < right._ticks;

    /// <summary>Less than or equal.</summary>
    public static bool operator <=(LrcTimestamp left, LrcTimestamp right) => left._ticks <= right._ticks;

    /// <summary>Greater than.</summary>
    public static bool operator >(LrcTimestamp left, LrcTimestamp right) => left._ticks > right._ticks;

    /// <summary>Greater than or equal.</summary>
    public static bool operator >=(LrcTimestamp left, LrcTimestamp right) => left._ticks >= right._ticks;

    /// <inheritdoc />
    public bool Equals(LrcTimestamp other) => _ticks == other._ticks;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LrcTimestamp t && Equals(t);

    /// <inheritdoc />
    public override int GetHashCode() => _ticks.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(LrcTimestamp other) => _ticks.CompareTo(other._ticks);
}
