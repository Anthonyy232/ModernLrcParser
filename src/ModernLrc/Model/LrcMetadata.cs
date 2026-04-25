namespace ModernLrc.Model;

/// <summary>Parsed document metadata: strongly-typed accessors for known ID tags
/// (last-wins on conflict in source order) plus the verbatim <see cref="RawTags"/>
/// collection containing every tag the parser encountered, in source order.</summary>
/// <remarks><para><see cref="Offset"/> is stored verbatim from the <c>[offset:N]</c> tag
/// and is NOT applied at parse time — line timestamps in the document remain unchanged.
/// Use <see cref="LrcDocumentExtensions.GetEffectiveTime"/> /
/// <see cref="LrcDocumentExtensions.FindLineAt"/> to factor it in. This preserves
/// round-trip fidelity: re-writing the document emits the original
/// <c>[offset:N]</c> tag.</para>
/// <para>The <c>tool</c> tag (used by some authoring software) maps to the same
/// <see cref="Tool"/> property as <c>re</c>; last-wins across both keys.</para></remarks>
public sealed record LrcMetadata
{
    /// <summary>Singleton empty metadata.</summary>
    public static LrcMetadata Empty { get; } = new();

    /// <summary>All ID tags in source order. Includes both the "winning" entry
    /// for each strongly-typed accessor and any duplicates / unknown keys.</summary>
    public EquatableArray<LrcTag> RawTags { get; init; } = EquatableArray<LrcTag>.Empty;

    /// <summary>Track title (<c>ti</c>).</summary>
    public string? Title { get; init; }

    /// <summary>Track artist (<c>ar</c>).</summary>
    public string? Artist { get; init; }

    /// <summary>Album (<c>al</c>).</summary>
    public string? Album { get; init; }

    /// <summary>File author / encoder (<c>au</c>).</summary>
    public string? Author { get; init; }

    /// <summary>Lyricist (<c>lr</c>).</summary>
    public string? Lyricist { get; init; }

    /// <summary>Created-by attribution (<c>by</c>).</summary>
    public string? CreatedBy { get; init; }

    /// <summary>Authoring tool (<c>re</c> or <c>tool</c>; last-wins across both keys).</summary>
    public string? Tool { get; init; }

    /// <summary>Format version (<c>ve</c>).</summary>
    public string? Version { get; init; }

    /// <summary>Track length (<c>length:mm:ss</c>) when known.</summary>
    public TimeSpan? Length { get; init; }

    /// <summary>Document timing offset (<c>offset:±N</c> milliseconds). Zero by default.</summary>
    public TimeSpan Offset { get; init; }
}

/// <summary>An ID tag in source order. <see cref="Key"/> is normalized to lowercase
/// invariant by the parser; <see cref="Value"/> is verbatim.</summary>
/// <param name="Key">Lowercase invariant tag key.</param>
/// <param name="Value">Verbatim tag value.</param>
public readonly record struct LrcTag(string Key, string Value);
