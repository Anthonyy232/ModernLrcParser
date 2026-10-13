using System.Globalization;

namespace ModernLrc.Model;

/// <summary>A known ID tag, i.e. one with a typed <see cref="LrcMetadata"/> property.</summary>
internal enum LrcMetadataField { Title, Artist, Album, Author, Lyricist, Length, CreatedBy, Offset, Tool, Version }

/// <summary>Key mapping, parsing and formatting for the known ID tags, shared by the parser,
/// the builder and the writer so all three agree.</summary>
internal static class LrcMetadataFields
{
    public const int Count = 10;

    public static LrcMetadataField? FromKey(string key) => key switch
    {
        "ti" => LrcMetadataField.Title,
        "ar" => LrcMetadataField.Artist,
        "al" => LrcMetadataField.Album,
        "au" => LrcMetadataField.Author,
        "lr" => LrcMetadataField.Lyricist,
        "length" => LrcMetadataField.Length,
        "by" => LrcMetadataField.CreatedBy,
        "offset" => LrcMetadataField.Offset,
        "re" or "tool" => LrcMetadataField.Tool,
        "ve" => LrcMetadataField.Version,
        _ => null,
    };

    /// <summary>Parse a raw tag value into the property's type. Only length and offset can fail.</summary>
    public static bool TryParse(LrcMetadataField field, string value, out object? parsed)
    {
        switch (field)
        {
            case LrcMetadataField.Length:
                bool isLength = LrcTimestamp.TryParse(value, CultureInfo.InvariantCulture, out var length);
                parsed = length.ToTimeSpan();
                return isLength;
            case LrcMetadataField.Offset:
                bool isOffset = long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long ms);
                parsed = TimeSpan.FromMilliseconds(ms);
                return isOffset;
            default:
                parsed = value;
                return true;
        }
    }

    /// <summary>The property value; for <see cref="LrcMetadataField.Offset"/> an unset value is <see cref="TimeSpan.Zero"/>.</summary>
    public static object? Get(LrcMetadata m, LrcMetadataField field) => field switch
    {
        LrcMetadataField.Title => m.Title,
        LrcMetadataField.Artist => m.Artist,
        LrcMetadataField.Album => m.Album,
        LrcMetadataField.Author => m.Author,
        LrcMetadataField.Lyricist => m.Lyricist,
        LrcMetadataField.Length => m.Length,
        LrcMetadataField.CreatedBy => m.CreatedBy,
        LrcMetadataField.Offset => m.Offset,
        LrcMetadataField.Tool => m.Tool,
        LrcMetadataField.Version => m.Version,
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    public static LrcMetadata With(LrcMetadata m, LrcMetadataField field, object? value) => field switch
    {
        LrcMetadataField.Title => m with { Title = (string?)value },
        LrcMetadataField.Artist => m with { Artist = (string?)value },
        LrcMetadataField.Album => m with { Album = (string?)value },
        LrcMetadataField.Author => m with { Author = (string?)value },
        LrcMetadataField.Lyricist => m with { Lyricist = (string?)value },
        LrcMetadataField.Length => m with { Length = (TimeSpan?)value },
        LrcMetadataField.CreatedBy => m with { CreatedBy = (string?)value },
        LrcMetadataField.Offset => m with { Offset = (TimeSpan?)value ?? TimeSpan.Zero },
        LrcMetadataField.Tool => m with { Tool = (string?)value },
        LrcMetadataField.Version => m with { Version = (string?)value },
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    /// <summary>The tag value to write, or null when the property is unset (a zero offset counts as unset).</summary>
    public static string? Format(LrcMetadata m, LrcMetadataField field) => Get(m, field) switch
    {
        TimeSpan length when field == LrcMetadataField.Length
            => string.Create(CultureInfo.InvariantCulture, $"{(long)length.TotalMinutes:D2}:{length.Seconds:D2}"),
        TimeSpan offset when offset != TimeSpan.Zero
            => string.Create(CultureInfo.InvariantCulture, $"{(long)offset.TotalMilliseconds:+0;-0}"),
        string text => text,
        _ => null,
    };

    /// <summary>True when re-parsing the raw tags for <paramref name="field"/> (last valid value
    /// wins, as in the parser) yields the current typed property.</summary>
    public static bool RawTagsAgree(LrcMetadata m, LrcMetadataField field)
    {
        object? implied = field == LrcMetadataField.Offset ? TimeSpan.Zero : null;
        foreach (var tag in m.RawTags)
        {
            if (FromKey(tag.Key) == field && TryParse(field, tag.Value, out var parsed))
                implied = parsed;
        }
        return Equals(implied, Get(m, field));
    }
}
