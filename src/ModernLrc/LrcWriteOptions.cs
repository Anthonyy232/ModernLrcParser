using System.Text;

namespace ModernLrc;

/// <summary>Line-ending style for writing.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design", "CA1028:Enum storage should be Int32",
    Justification = "byte is intentional — option records are small and dense.")]
public enum LrcLineEnding : byte
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
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design", "CA1028:Enum storage should be Int32",
    Justification = "byte is intentional — option records are small and dense.")]
public enum LrcTimestampPrecision : byte
{
    /// <summary><c>mm:ss.xx</c> — most common.</summary>
    Centiseconds = 0,

    /// <summary><c>mm:ss.xxx</c> — millisecond precision.</summary>
    Milliseconds = 1,
}

/// <summary>Ordering policy for the metadata block.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design", "CA1028:Enum storage should be Int32",
    Justification = "byte is intentional — option records are small and dense.")]
public enum LrcMetadataOrdering : byte
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
    private static readonly Encoding UTF8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Default options.</summary>
    public static LrcWriteOptions Default { get; } = new();

    /// <summary>Line-ending style (default <see cref="LrcLineEnding.Lf"/>).</summary>
    public LrcLineEnding LineEnding { get; init; } = LrcLineEnding.Lf;

    /// <summary>Output encoding for byte sinks (default UTF-8 with no BOM).</summary>
    public Encoding Encoding { get; init; } = UTF8NoBom;

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

    /// <summary>Floor for the initial output buffer (chars for char sinks, bytes for byte
    /// sinks). Must be ≥ 16. Default 4096. The string, TextWriter, TryWrite, and Stream
    /// sinks may stage into a larger buffer when the document's estimated size exceeds this
    /// value, so they don't waste cycles doubling-and-copying through several growth steps
    /// on a large render. The <see cref="System.Buffers.IBufferWriter{T}"/> sinks honor this
    /// value verbatim, since the buffer is caller-owned.</summary>
    public int InitialBufferSize
    {
        get => _initialBufferSize;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 16);
            _initialBufferSize = value;
        }
    }
    private readonly int _initialBufferSize = 4096;
}
