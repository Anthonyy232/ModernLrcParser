using System.Text;

namespace ModernLrc;

/// <summary>Line-ending style for writing.</summary>
public enum LrcLineEnding
{
    /// <summary>LF (<c>\n</c>) — default and recommended.</summary>
    Lf = 0,

    /// <summary>CRLF (<c>\r\n</c>).</summary>
    Crlf = 1,

    /// <summary>CR (<c>\r</c>) — legacy.</summary>
    Cr = 2,

    /// <summary>Resolves to <see cref="Environment.NewLine"/>; falls back to <see cref="Lf"/>
    /// if not LF/CRLF/CR.</summary>
    System = 3,
}

/// <summary>Timestamp precision for writing.</summary>
public enum LrcTimestampPrecision
{
    /// <summary><c>mm:ss.xx</c> — most common.</summary>
    Centiseconds = 0,

    /// <summary><c>mm:ss.xxx</c> — millisecond precision.</summary>
    Milliseconds = 1,
}

/// <summary>Ordering policy for the metadata block.</summary>
public enum LrcMetadataOrdering
{
    /// <summary>Typed accessors with no matching raw tag in
    /// <c>ti, ar, al, au, lr, length, by, offset, re, ve</c> order, then
    /// <c>RawTags</c> in array order.</summary>
    Canonical = 0,

    /// <summary>Typed accessors with no matching raw tag A→Z by name, then
    /// <c>RawTags</c> A→Z by key.</summary>
    Alphabetical = 1,
}

/// <summary>Configuration for a write operation.</summary>
/// <example>
/// <code>
/// // Emit a UTF-8 BOM and use CRLF line endings (Windows-friendly).
/// var winFriendly = new LrcWriteOptions
/// {
///     EmitByteOrderMark = true,
///     LineEnding = LrcLineEnding.Crlf,
/// };
///
/// // Millisecond precision and alphabetical metadata.
/// var precise = new LrcWriteOptions
/// {
///     TimestampPrecision = LrcTimestampPrecision.Milliseconds,
///     MetadataOrdering = LrcMetadataOrdering.Alphabetical,
/// };
///
/// // Emit one timestamp per line instead of collapsing into [t1][t2]text groups.
/// var verbose = new LrcWriteOptions { CollapseIdenticalLines = false };
/// </code>
/// </example>
public sealed record LrcWriteOptions
{
    /// <summary>Default options.</summary>
    public static LrcWriteOptions Default { get; } = new();

    /// <summary>Line-ending style (default <see cref="LrcLineEnding.Lf"/>).</summary>
    public LrcLineEnding LineEnding { get; init; } = LrcLineEnding.Lf;

    /// <summary>Output encoding for byte sinks (default UTF-8). Whether its BOM is written is
    /// controlled by <see cref="EmitByteOrderMark"/> alone.</summary>
    public Encoding Encoding { get; init; } = Encoding.UTF8;

    /// <summary>Sole BOM control. <c>true</c> prepends <see cref="System.Text.Encoding.GetPreamble"/>;
    /// <c>false</c> emits no preamble regardless of the encoding's natural BOM. Default <c>false</c>.</summary>
    public bool EmitByteOrderMark { get; init; }

    /// <summary>Append a final newline (default <c>true</c>).</summary>
    public bool TrailingNewline { get; init; } = true;

    /// <summary>Timestamp precision (default centiseconds).</summary>
    public LrcTimestampPrecision TimestampPrecision { get; init; } = LrcTimestampPrecision.Centiseconds;

    /// <summary>Collapse consecutive lines with identical content (text + voice + line type) into
    /// a single multi-timestamp line (<c>[t1][t2]text</c>). Default <c>true</c> — this is the
    /// round-trip mechanism for inputs originally written as multi-timestamp groups, since the
    /// parser fans them out into one line per timestamp.</summary>
    public bool CollapseIdenticalLines { get; init; } = true;

    /// <summary>Emit voice markers (default <c>true</c>). When <c>false</c>, voice metadata is dropped.</summary>
    public bool EmitVoiceMarkers { get; init; } = true;

    /// <summary>Only emit a voice marker on transition (default <c>true</c>); when <c>false</c>,
    /// every line carrying a non-default voice receives an explicit marker.</summary>
    public bool VoiceMarkerOnChangeOnly { get; init; } = true;

    /// <summary>Ordering policy for the metadata block.</summary>
    public LrcMetadataOrdering MetadataOrdering { get; init; } = LrcMetadataOrdering.Canonical;
}
