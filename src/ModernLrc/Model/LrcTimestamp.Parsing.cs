using System.Globalization;

namespace ModernLrc.Model;

public readonly partial struct LrcTimestamp
{
    /// <summary>Parse <c>"mm:ss.xx"</c>, <c>"mm:ss.xxx"</c>, <c>"mm:ss"</c>, or <c>"mm:ss:xx"</c>.
    /// Minutes unbounded; hours notation NOT accepted; negative input rejected. Always uses
    /// <see cref="CultureInfo.InvariantCulture"/>.</summary>
    public static LrcTimestamp Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        if (TryParse(s, provider, out var result)) return result;
        throw new FormatException($"Input '{s.ToString()}' is not a valid LRC timestamp.");
    }

    /// <summary>Parse from a string. Throws on null.</summary>
    public static LrcTimestamp Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>Try-parse a string. Null returns false.</summary>
    public static bool TryParse(string? s, IFormatProvider? provider, out LrcTimestamp result)
    {
        if (s is null) { result = default; return false; }
        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>Try-parse a span (canonical core).</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out LrcTimestamp result)
    {
        result = default;
        if (s.IsEmpty) return false;
        if (s[0] == '-') return false;

        int colon1 = s.IndexOf(':');
        if (colon1 <= 0) return false;

        // Detect three-segment hours notation: h:mm:ss[.fff] or h:mm:ss[,fff]
        // Only when the second segment is followed by another ':' before any '.' or ',' separator,
        // AND the first (hours) segment is non-zero. When hours==0, "00:mm:ss" is indistinguishable
        // from the legacy colon-fraction form "mm:ss:ff", so we fall through to that parser.
        var afterFirst = s[(colon1 + 1)..];
        int colon2 = afterFirst.IndexOf(':');
        int dotOrCommaInAfterFirst = afterFirst.IndexOfAny('.', ',');
        if (colon2 >= 0 && (dotOrCommaInAfterFirst < 0 || colon2 < dotOrCommaInAfterFirst))
        {
            // Only try hours form when the first segment is non-zero.
            bool firstNonZero = false;
            for (int i = 0; i < colon1; i++) { if (s[i] != '0') { firstNonZero = true; break; } }
            if (firstNonZero && TryParseHoursMinutesSeconds(s, colon1, out result))
                return true;
            // Fall through to default parse if the three-segment shape didn't produce a valid value.
        }

        if (!long.TryParse(s[..colon1], NumberStyles.None, CultureInfo.InvariantCulture, out long minutes))
            return false;
        // Sanity cap matches the hours-form cap: ~100 years' worth of minutes (24*365*100*60).
        // Beyond this, the subsequent ticks multiplication could silently overflow.
        if (minutes < 0 || minutes > 24L * 365 * 100 * 60) return false;

        var rest = s[(colon1 + 1)..];
        if (rest.IsEmpty) return false;

        int sepIdx = rest.IndexOfAny('.', ':', ',');
        ReadOnlySpan<char> secondsSpan;
        ReadOnlySpan<char> fractionSpan = default;
        if (sepIdx < 0)
        {
            secondsSpan = rest;
        }
        else
        {
            secondsSpan = rest[..sepIdx];
            fractionSpan = rest[(sepIdx + 1)..];
            if (fractionSpan.IsEmpty) return false;
        }

        if (!int.TryParse(secondsSpan, NumberStyles.None, CultureInfo.InvariantCulture, out int seconds))
            return false;
        if (seconds < 0 || seconds >= 60) return false;

        long ticks = minutes * TimeSpan.TicksPerMinute + seconds * TimeSpan.TicksPerSecond;

        if (!fractionSpan.IsEmpty)
        {
            if (!int.TryParse(fractionSpan, NumberStyles.None, CultureInfo.InvariantCulture, out int frac))
                return false;
            if (frac < 0) return false;

            int len = fractionSpan.Length;
            long fracTicks = len switch
            {
                1 => frac * (TimeSpan.TicksPerSecond / 10),
                2 => frac * (TimeSpan.TicksPerSecond / 100),
                3 => frac * TimeSpan.TicksPerMillisecond,
                _ => TruncateToMs(frac, len),
            };
            ticks += fracTicks;
        }

        result = new LrcTimestamp(ticks);
        return true;
    }

    private static long TruncateToMs(int frac, int len)
    {
        long divisor = 1;
        for (int i = 0; i < len - 3; i++) divisor *= 10;
        return (frac / divisor) * TimeSpan.TicksPerMillisecond;
    }

    private static bool TryParseHoursMinutesSeconds(ReadOnlySpan<char> s, int colon1, out LrcTimestamp result)
    {
        result = default;
        if (!long.TryParse(s[..colon1], NumberStyles.None, CultureInfo.InvariantCulture, out long hours))
            return false;
        if (hours < 0 || hours > 24L * 365 * 100) return false; // sanity cap

        var rest = s[(colon1 + 1)..];
        int colon2 = rest.IndexOf(':');
        if (colon2 <= 0) return false;
        if (!int.TryParse(rest[..colon2], NumberStyles.None, CultureInfo.InvariantCulture, out int minutes))
            return false;
        if (minutes < 0 || minutes >= 60) return false;

        var third = rest[(colon2 + 1)..];
        if (third.IsEmpty) return false;

        // Third segment may be ss, ss.fff, ss,fff, or ss:fff (legacy).
        int sepIdx = third.IndexOfAny('.', ',', ':');
        ReadOnlySpan<char> secondsSpan;
        ReadOnlySpan<char> fractionSpan = default;
        if (sepIdx < 0) secondsSpan = third;
        else { secondsSpan = third[..sepIdx]; fractionSpan = third[(sepIdx + 1)..]; if (fractionSpan.IsEmpty) return false; }

        if (!int.TryParse(secondsSpan, NumberStyles.None, CultureInfo.InvariantCulture, out int seconds))
            return false;
        if (seconds < 0 || seconds >= 60) return false;

        long ticks = hours * TimeSpan.TicksPerHour
                   + minutes * TimeSpan.TicksPerMinute
                   + seconds * TimeSpan.TicksPerSecond;

        if (!fractionSpan.IsEmpty)
        {
            if (!int.TryParse(fractionSpan, NumberStyles.None, CultureInfo.InvariantCulture, out int frac))
                return false;
            if (frac < 0) return false;
            int len = fractionSpan.Length;
            long fracTicks = len switch
            {
                1 => frac * (TimeSpan.TicksPerSecond / 10),
                2 => frac * (TimeSpan.TicksPerSecond / 100),
                3 => frac * TimeSpan.TicksPerMillisecond,
                _ => TruncateToMs(frac, len),
            };
            ticks += fracTicks;
        }

        result = new LrcTimestamp(ticks);
        return true;
    }

    static LrcTimestamp IParsable<LrcTimestamp>.Parse(string s, IFormatProvider? provider) => Parse(s, provider);

    static bool IParsable<LrcTimestamp>.TryParse(string? s, IFormatProvider? provider, out LrcTimestamp result)
        => TryParse(s, provider, out result);

    static LrcTimestamp ISpanParsable<LrcTimestamp>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => Parse(s, provider);

    static bool ISpanParsable<LrcTimestamp>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out LrcTimestamp result)
        => TryParse(s, provider, out result);

    /// <inheritdoc />
    public override string ToString() => ToString("G", CultureInfo.InvariantCulture);

    /// <summary>Format using a code: <c>"G"</c> (default, <c>mm:ss.xx</c>), <c>"F"</c> (<c>mm:ss.xxx</c>),
    /// <c>"S"</c> (<c>mm:ss</c>), <c>"B"</c> (<c>[mm:ss.xx]</c>), <c>"W"</c> (<c>&lt;mm:ss.xx&gt;</c>).</summary>
    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        Span<char> buf = stackalloc char[24];
        return TryFormat(buf, out int written, format.AsSpan(), formatProvider)
            ? new string(buf[..written])
            : throw new FormatException($"Format '{format}' is not supported.");
    }

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        charsWritten = 0;
        char code = format.IsEmpty ? 'G' : format[0];
        if (code == 'B' || code == 'W')
        {
            char open = code == 'B' ? '[' : '<';
            char close = code == 'B' ? ']' : '>';
            Span<char> tmp = stackalloc char[24];
            tmp[0] = open;
            if (!TryFormatCore(tmp[1..], out int innerLen, 'G'))
            { return false; }
            int needed = innerLen + 2;
            tmp[1 + innerLen] = close;
            if (destination.Length < needed) return false;
            tmp[..needed].CopyTo(destination);
            charsWritten = needed;
            return true;
        }
        return TryFormatCore(destination, out charsWritten, code);
    }

    /// <inheritdoc />
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        Span<char> tmp = stackalloc char[24];
        if (!TryFormat(tmp, out int tmpCharsWritten, format, provider))
        {
            bytesWritten = 0;
            return false;
        }
        // Timestamps are pure ASCII (0-9, ':', '.', '[', ']', '<', '>'), so each char maps 1:1 to a byte.
        if (utf8Destination.Length < tmpCharsWritten)
        {
            bytesWritten = 0;
            return false;
        }
        for (int i = 0; i < tmpCharsWritten; i++)
            utf8Destination[i] = (byte)tmp[i];
        bytesWritten = tmpCharsWritten;
        return true;
    }

    private bool TryFormatCore(Span<char> destination, out int charsWritten, char code)
    {
        long totalSec = _ticks / TimeSpan.TicksPerSecond;
        long minutes = totalSec / 60;
        int seconds = (int)(totalSec % 60);
        long fracTicks = _ticks % TimeSpan.TicksPerSecond;

        Span<char> work = stackalloc char[24];
        int pos = 0;

        if (!minutes.TryFormat(work[pos..], out int mWritten, "00", CultureInfo.InvariantCulture))
        { charsWritten = 0; return false; }
        pos += mWritten;
        work[pos++] = ':';

        if (!seconds.TryFormat(work[pos..], out int sWritten, "00", CultureInfo.InvariantCulture))
        { charsWritten = 0; return false; }
        pos += sWritten;

        if (code == 'S')
        {
            if (destination.Length < pos) { charsWritten = 0; return false; }
            work[..pos].CopyTo(destination);
            charsWritten = pos;
            return true;
        }

        work[pos++] = '.';

        if (code == 'F')
        {
            int millis = (int)(fracTicks / TimeSpan.TicksPerMillisecond);
            if (!millis.TryFormat(work[pos..], out int fWritten, "000", CultureInfo.InvariantCulture))
            { charsWritten = 0; return false; }
            pos += fWritten;
        }
        else // 'G' default
        {
            int centi = (int)(fracTicks / (TimeSpan.TicksPerSecond / 100));
            if (!centi.TryFormat(work[pos..], out int fWritten, "00", CultureInfo.InvariantCulture))
            { charsWritten = 0; return false; }
            pos += fWritten;
        }

        if (destination.Length < pos) { charsWritten = 0; return false; }
        work[..pos].CopyTo(destination);
        charsWritten = pos;
        return true;
    }
}
