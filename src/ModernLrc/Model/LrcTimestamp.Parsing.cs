using System.Globalization;
using System.Text;

namespace ModernLrc.Model;

/// <summary>Side-output of <see cref="LrcTimestamp.TryParseWithShape"/> — the parsed value
/// plus the shape information the scanner uses to drive its <c>LRC0030</c> /
/// <c>LRC0031</c> diagnostics without re-walking the input.</summary>
internal readonly record struct LrcTimestampShape(LrcTimestamp Value, bool IsCanonical, int FractionLength);

public readonly partial struct LrcTimestamp
{
    /// <summary>Upper bound on hours (~100 years), so tick arithmetic cannot overflow.</summary>
    private const long MaxHours = 24L * 365 * 100;

    /// <summary>Parse an LRC timestamp: <c>mm:ss</c> with an optional fraction after <c>.</c>,
    /// <c>,</c> or (legacy) <c>:</c>, or hours notation <c>h:mm:ss[.fff]</c>. Minutes are unbounded
    /// in the two-segment form; fractions beyond milliseconds are truncated; negative input is
    /// rejected. Always uses <see cref="CultureInfo.InvariantCulture"/>.</summary>
    public static LrcTimestamp Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
        => TryParse(s, provider, out var result)
            ? result
            : throw new FormatException($"Input '{s.ToString()}' is not a valid LRC timestamp.");

    /// <summary>Parse from a string. Throws on null.</summary>
    public static LrcTimestamp Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>Try-parse a string. Null returns false.</summary>
    public static bool TryParse(string? s, IFormatProvider? provider, out LrcTimestamp result)
        => TryParse(s.AsSpan(), provider, out result);

    /// <summary>Try-parse a span.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out LrcTimestamp result)
    {
        bool ok = TryParseWithShape(s, out var shape);
        result = shape.Value;
        return ok;
    }

    /// <summary>Try-parse a span and report its shape, so the scanner can drive its
    /// <c>LRC0030</c> / <c>LRC0031</c> diagnostics without a second pass.</summary>
    internal static bool TryParseWithShape(ReadOnlySpan<char> s, out LrcTimestampShape shape)
    {
        shape = default;
        int fractionSeparator = s.IndexOfAny('.', ',');
        bool hasDecimal = fractionSeparator >= 0;
        var clock = hasDecimal ? s[..fractionSeparator] : s;
        var decimals = hasDecimal ? s[(fractionSeparator + 1)..] : default;

        Span<Range> parts = stackalloc Range[5];
        int count = clock.Split(parts, ':');

        // Hours notation: h:mm:ss[.fff] or h:mm:ss:fff. With zero hours, "00:mm:ss" reads as
        // the legacy mm:ss:ff form instead, as does anything the hours form rejects.
        if (count is 3 or 4 && !(count == 4 && hasDecimal) && clock[parts[0]].ContainsAnyExcept('0')
            && TryCompose(clock[parts[0]], clock[parts[1]], clock[parts[2]],
                count == 4 ? clock[parts[3]] : decimals, hasFraction: count == 4 || hasDecimal,
                out var value, out int fractionLength))
        {
            shape = new LrcTimestampShape(value, IsCanonical: false, fractionLength);
            return true;
        }

        bool legacyColonFraction = count == 3 && !hasDecimal;
        if (count != 2 && !legacyColonFraction) return false;
        if (!TryCompose(default, clock[parts[0]], clock[parts[1]],
                legacyColonFraction ? clock[parts[2]] : decimals, hasFraction: legacyColonFraction || hasDecimal,
                out value, out fractionLength))
            return false;

        bool isCanonical = hasDecimal && s[fractionSeparator] == '.' && fractionLength == 2;
        shape = new LrcTimestampShape(value, isCanonical, fractionLength);
        return true;
    }

    /// <summary>Combine the segments; <paramref name="hours"/> is empty in the two-segment form,
    /// where minutes are unbounded. <paramref name="fractionLength"/> is -1 without a fraction.</summary>
    private static bool TryCompose(
        ReadOnlySpan<char> hours, ReadOnlySpan<char> minutes, ReadOnlySpan<char> seconds,
        ReadOnlySpan<char> fraction, bool hasFraction, out LrcTimestamp value, out int fractionLength)
    {
        value = default;
        fractionLength = hasFraction ? fraction.Length : -1;
        long h = 0, frac = 0;
        if (!hours.IsEmpty && !TryParseDigits(hours, MaxHours, out h)) return false;
        if (!TryParseDigits(minutes, hours.IsEmpty ? MaxHours * 60 : 59, out long m)) return false;
        if (!TryParseDigits(seconds, 59, out long sec)) return false;
        if (hasFraction && !TryParseDigits(fraction, int.MaxValue, out frac)) return false;

        // Scale the fraction to exactly three digits (milliseconds), truncating extra precision.
        for (int i = fraction.Length; i > 3; i--) frac /= 10;
        for (int i = fraction.Length; i < 3; i++) frac *= 10;

        value = new LrcTimestamp(((h * 60 + m) * 60 + sec) * TimeSpan.TicksPerSecond + frac * TimeSpan.TicksPerMillisecond);
        return true;
    }

    private static bool TryParseDigits(ReadOnlySpan<char> s, long max, out long value)
        => long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value <= max;

    static LrcTimestamp IParsable<LrcTimestamp>.Parse(string s, IFormatProvider? provider) => Parse(s, provider);

    static bool IParsable<LrcTimestamp>.TryParse(string? s, IFormatProvider? provider, out LrcTimestamp result)
        => TryParse(s, provider, out result);

    static LrcTimestamp ISpanParsable<LrcTimestamp>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => Parse(s, provider);

    static bool ISpanParsable<LrcTimestamp>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out LrcTimestamp result)
        => TryParse(s, provider, out result);

    /// <summary>Longest possible output: <c>[</c> + 11-digit minutes + <c>:ss.fff]</c>.</summary>
    private const int MaxFormattedLength = 32;

    /// <inheritdoc />
    public override string ToString() => ToString("G", CultureInfo.InvariantCulture);

    /// <summary>Format using a code: <c>"G"</c> (default, <c>mm:ss.xx</c>), <c>"F"</c> (<c>mm:ss.xxx</c>),
    /// <c>"S"</c> (<c>mm:ss</c>), <c>"B"</c> (<c>[mm:ss.xx]</c>), <c>"W"</c> (<c>&lt;mm:ss.xx&gt;</c>).</summary>
    /// <exception cref="FormatException"><paramref name="format"/> is not one of the codes above.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        Span<char> buf = stackalloc char[MaxFormattedLength];
        TryFormat(buf, out int written, format.AsSpan(), formatProvider);
        return new string(buf[..written]);
    }

    /// <inheritdoc />
    /// <exception cref="FormatException"><paramref name="format"/> is not a supported code.</exception>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        char code = format.IsEmpty ? 'G' : format[0];
        long totalSeconds = _ticks / TimeSpan.TicksPerSecond;
        long minutes = totalSeconds / 60;
        long seconds = totalSeconds % 60;
        long fracTicks = _ticks % TimeSpan.TicksPerSecond;
        long centis = fracTicks / (TimeSpan.TicksPerSecond / 100);
        long millis = fracTicks / TimeSpan.TicksPerMillisecond;
        var ic = CultureInfo.InvariantCulture;

        return code switch
        {
            'G' => destination.TryWrite(ic, $"{minutes:00}:{seconds:00}.{centis:00}", out charsWritten),
            'F' => destination.TryWrite(ic, $"{minutes:00}:{seconds:00}.{millis:000}", out charsWritten),
            'S' => destination.TryWrite(ic, $"{minutes:00}:{seconds:00}", out charsWritten),
            'B' => destination.TryWrite(ic, $"[{minutes:00}:{seconds:00}.{centis:00}]", out charsWritten),
            'W' => destination.TryWrite(ic, $"<{minutes:00}:{seconds:00}.{centis:00}>", out charsWritten),
            _ => throw new FormatException($"Format '{format.ToString()}' is not supported."),
        };
    }

    /// <inheritdoc />
    /// <exception cref="FormatException"><paramref name="format"/> is not a supported code.</exception>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        Span<char> chars = stackalloc char[MaxFormattedLength];
        TryFormat(chars, out int charsWritten, format, provider);
        return Encoding.UTF8.TryGetBytes(chars[..charsWritten], utf8Destination, out bytesWritten);
    }
}
