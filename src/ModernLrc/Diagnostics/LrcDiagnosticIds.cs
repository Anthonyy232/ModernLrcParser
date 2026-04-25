namespace ModernLrc.Diagnostics;

/// <summary>String identifiers for every diagnostic the parser or writer can emit.
/// Exposed as <c>const string</c> so consumers can switch on them without coupling
/// to an enum that changes between versions.</summary>
public static class LrcDiagnosticIds
{
    /// <summary>Unclosed <c>[…</c> tag.</summary>
    public const string UnclosedTag = "LRC0001";

    /// <summary>Invalid timestamp format.</summary>
    public const string InvalidTimestamp = "LRC0002";

    /// <summary>Malformed ID tag.</summary>
    public const string MalformedIdTag = "LRC0003";

    /// <summary>Unknown ID tag preserved as raw.</summary>
    public const string UnknownIdTag = "LRC0004";

    /// <summary>Invalid <c>offset</c> value.</summary>
    public const string InvalidOffset = "LRC0005";

    /// <summary>Invalid <c>length</c> value.</summary>
    public const string InvalidLength = "LRC0006";

    /// <summary>Invalid enhanced word timestamp.</summary>
    public const string InvalidEnhancedTimestamp = "LRC0007";

    /// <summary>Unclosed enhanced timestamp.</summary>
    public const string UnclosedEnhancedTimestamp = "LRC0008";

    /// <summary>Empty timestamp <c>[]</c>.</summary>
    public const string EmptyTimestamp = "LRC0009";

    /// <summary>Encoding fallback used (Error severity — callers must opt in to fallback).</summary>
    public const string EncodingFallback = "LRC0010";

    /// <summary>Conflicting metadata values for the same key.</summary>
    public const string ConflictingMetadata = "LRC0020";

    /// <summary>Duplicate identical metadata.</summary>
    public const string DuplicateMetadata = "LRC0021";

    /// <summary>Non-standard timestamp variant accepted.</summary>
    public const string NonStandardTimestamp = "LRC0030";

    /// <summary>Sub-millisecond precision truncated.</summary>
    public const string TruncatedPrecision = "LRC0031";

    /// <summary>Free text without a timestamp dropped.</summary>
    public const string DroppedUntimedText = "LRC0050";

    /// <summary>Timestamp without following text.</summary>
    public const string TimestampWithoutText = "LRC0051";

    /// <summary>Lines reordered to maintain sort guarantee.</summary>
    public const string LinesReordered = "LRC0060";

    /// <summary>Implausible timestamp (&gt; 24h) — value still parsed.</summary>
    public const string ImplausibleTimestamp = "LRC0070";

    /// <summary>Empty enhanced word (back-to-back <c>&lt;&gt;</c> markers).</summary>
    public const string EmptyEnhancedWord = "LRC0080";

    /// <summary>Unrepresentable voice transition (Walaoke has no "clear voice" marker) — write side.</summary>
    public const string UnrepresentableVoiceTransition = "LRC0090";

    /// <summary>Non-timestamp <c>[…]</c> group treated as content text.</summary>
    public const string BracketedContentTolerance = "LRC0091";

    /// <summary>ID3 language prefix (e.g., <c>eng||</c>) was stripped from the input.</summary>
    public const string Id3LanguagePrefixStripped = "LRC0092";

    /// <summary><c>MaxDiagnostics</c> reached; remainder suppressed.</summary>
    public const string MaxDiagnosticsReached = "LRC0099";
}
