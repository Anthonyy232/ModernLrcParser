using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using ModernLrc.Internal;
using ModernLrc.Model;

namespace ModernLrc;

/// <summary>Mutable builder for <see cref="LrcDocument"/>. Maintains insertion order;
/// <see cref="Build"/> sorts and freezes. Single-threaded contract — concurrent use
/// from multiple threads is undefined (mirrors <see cref="System.Text.StringBuilder"/>).</summary>
/// <remarks><see cref="Build"/> is idempotent: it does not mutate the builder, so the
/// same builder instance can keep adding lines and produce updated documents.</remarks>
/// <example>
/// <code>
/// using ModernLrc;
/// using ModernLrc.Model;
///
/// var doc = new LrcDocumentBuilder()
///     .WithTitle("Song")
///     .WithArtist("Artist")
///     .WithOffset(TimeSpan.FromMilliseconds(-150))
///     .AddLine("00:01.00", "intro")
///     .AddLine("00:05.00", "she sings", LrcVoice.Female)
///     .AddEnhancedLine(
///         LrcTimestamp.FromMilliseconds(10_000),
///         [(LrcTimestamp.FromMilliseconds(10_000), "word "),
///          (LrcTimestamp.FromMilliseconds(10_500), "by word")])
///     .Build();
/// </code>
/// </example>
public sealed class LrcDocumentBuilder
{
    private readonly List<LrcLine> _lines = new();
    private readonly List<LrcTag> _rawTags = new();

    private LrcMetadata _metadata = LrcMetadata.Empty;

    /// <summary>Empty builder.</summary>
    public LrcDocumentBuilder() { }

    /// <summary>Seed from an existing document (shallow copy — records are immutable so sharing is safe).</summary>
    public LrcDocumentBuilder(LrcDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _metadata = source.Metadata;
        _rawTags.AddRange(source.Metadata.RawTags);
        _lines.AddRange(source.Lines);
    }

    /// <summary>Number of lines currently held (in insertion order).</summary>
    public int LineCount => _lines.Count;

    /// <summary>Get a line in insertion order. Use <see cref="Build"/> to retrieve sorted output.</summary>
    public LrcLine GetLineAt(int index) => _lines[index];

    /// <summary>Set or clear the title (<c>ti</c>).</summary>
    public LrcDocumentBuilder WithTitle(string? value) => Set(LrcMetadataField.Title, value);

    /// <summary>Set or clear the artist (<c>ar</c>).</summary>
    public LrcDocumentBuilder WithArtist(string? value) => Set(LrcMetadataField.Artist, value);

    /// <summary>Set or clear the album (<c>al</c>).</summary>
    public LrcDocumentBuilder WithAlbum(string? value) => Set(LrcMetadataField.Album, value);

    /// <summary>Set or clear the author (<c>au</c>).</summary>
    public LrcDocumentBuilder WithAuthor(string? value) => Set(LrcMetadataField.Author, value);

    /// <summary>Set or clear the lyricist (<c>lr</c>).</summary>
    public LrcDocumentBuilder WithLyricist(string? value) => Set(LrcMetadataField.Lyricist, value);

    /// <summary>Set or clear the created-by attribution (<c>by</c>).</summary>
    public LrcDocumentBuilder WithCreatedBy(string? value) => Set(LrcMetadataField.CreatedBy, value);

    /// <summary>Set or clear the tool (<c>re</c> / <c>tool</c>).</summary>
    public LrcDocumentBuilder WithTool(string? value) => Set(LrcMetadataField.Tool, value);

    /// <summary>Set or clear the version (<c>ve</c>).</summary>
    public LrcDocumentBuilder WithVersion(string? value) => Set(LrcMetadataField.Version, value);

    /// <summary>Set or clear the track length (<c>length:mm:ss</c>).</summary>
    public LrcDocumentBuilder WithLength(TimeSpan? value) => Set(LrcMetadataField.Length, value);

    /// <summary>Set the document offset (<c>offset:±N</c> ms).</summary>
    public LrcDocumentBuilder WithOffset(TimeSpan value) => Set(LrcMetadataField.Offset, value);

    /// <summary>Append a raw tag entry (preserves insertion order).
    /// <paramref name="key"/> must be non-empty (whitespace-only is rejected — it would round-trip
    /// to <c>[ :value]</c> which the parser cannot read back). As in the parser, a known key
    /// (<c>ti</c>, <c>length</c>, …) also sets the matching typed property.</summary>
    public LrcDocumentBuilder WithRawTag(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _rawTags.Add(new LrcTag(key, value));
        if (LrcMetadataFields.FromKey(key) is { } field && LrcMetadataFields.TryParse(field, value, out var parsed))
            _metadata = LrcMetadataFields.With(_metadata, field, parsed);
        return this;
    }

    /// <summary>Remove every raw tag whose key matches (ordinal).</summary>
    public LrcDocumentBuilder RemoveRawTag(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _rawTags.RemoveAll(t => string.Equals(t.Key, key, StringComparison.Ordinal));
        return this;
    }

    /// <summary>Set a typed property and drop the raw tags it replaces.</summary>
    private LrcDocumentBuilder Set(LrcMetadataField field, object? value)
    {
        _metadata = LrcMetadataFields.With(_metadata, field, value);
        _rawTags.RemoveAll(t => LrcMetadataFields.FromKey(t.Key) == field);
        return this;
    }

    /// <summary>Add a plain line whose timestamp is parsed from <paramref name="timestamp"/>.</summary>
    public LrcDocumentBuilder AddLine(string timestamp, string text, LrcVoice voice = LrcVoice.Default)
        => AddLine(LrcTimestamp.Parse(timestamp, CultureInfo.InvariantCulture), text, voice);

    /// <summary>Add a plain line at <paramref name="timestamp"/>.</summary>
    public LrcDocumentBuilder AddLine(LrcTimestamp timestamp, string text, LrcVoice voice = LrcVoice.Default)
        => AddLineGroup([timestamp], text, voice);

    /// <summary>Add a plain line at <paramref name="timestamp"/> (TimeSpan overload).</summary>
    public LrcDocumentBuilder AddLine(TimeSpan timestamp, string text, LrcVoice voice = LrcVoice.Default)
        => AddLine(LrcTimestamp.FromTimeSpan(timestamp), text, voice);

    /// <summary>Add the same plain text at multiple timestamps. Each timestamp produces its own
    /// <see cref="LrcPlainLine"/> sharing the same <paramref name="text"/> reference. The writer's
    /// <see cref="LrcWriteOptions.CollapseIdenticalLines"/> setting controls whether they re-emit
    /// as a <c>[t1][t2]text</c> group.</summary>
    public LrcDocumentBuilder AddLineGroup(ReadOnlySpan<LrcTimestamp> timestamps, string text, LrcVoice voice = LrcVoice.Default)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (timestamps.IsEmpty) throw new ArgumentException("At least one timestamp is required.", nameof(timestamps));
        foreach (var t in timestamps)
            _lines.Add(new LrcPlainLine { Timestamp = t, Text = text, EffectiveVoice = voice });
        return this;
    }

    /// <summary>Add an enhanced (word-timed) line at a single timestamp.</summary>
    public LrcDocumentBuilder AddEnhancedLine(LrcTimestamp lineTimestamp, ReadOnlySpan<LrcWord> words, LrcVoice voice = LrcVoice.Default)
        => AddEnhancedLineGroup([lineTimestamp], words, voice);

    /// <summary>Add an enhanced (word-timed) line from a tuple sequence.</summary>
    public LrcDocumentBuilder AddEnhancedLine(LrcTimestamp lineTimestamp, IEnumerable<(LrcTimestamp Time, string Text)> words, LrcVoice voice = LrcVoice.Default)
    {
        ArgumentNullException.ThrowIfNull(words);
        return AddEnhancedLine(lineTimestamp, words.Select(static w => new LrcWord(w.Time, w.Text)).ToArray(), voice);
    }

    /// <summary>Add the same enhanced line content at multiple top-level timestamps. Each timestamp
    /// produces its own <see cref="LrcEnhancedLine"/> sharing the same <paramref name="words"/>.</summary>
    public LrcDocumentBuilder AddEnhancedLineGroup(ReadOnlySpan<LrcTimestamp> lineTimestamps, ReadOnlySpan<LrcWord> words, LrcVoice voice = LrcVoice.Default)
    {
        if (lineTimestamps.IsEmpty)
            throw new ArgumentException("At least one timestamp is required.", nameof(lineTimestamps));
        foreach (var w in words)
        {
            // default(LrcWord) has a null Text.
            if (w.Text is null) throw new ArgumentException("LrcWord.Text cannot be null.", nameof(words));
        }
        var shared = EquatableArray.Create(words);
        foreach (var t in lineTimestamps)
            _lines.Add(new LrcEnhancedLine { Timestamp = t, Words = shared, EffectiveVoice = voice });
        return this;
    }

    /// <summary>Append a fully-constructed <see cref="LrcLine"/> (reference held; records are immutable).</summary>
    public LrcDocumentBuilder AddLine(LrcLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        _lines.Add(line);
        return this;
    }

    /// <summary>Append every line from a sequence.</summary>
    public LrcDocumentBuilder AddLines(IEnumerable<LrcLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        foreach (var line in lines)
            AddLine(line);
        return this;
    }

    /// <summary>Drop both lines and metadata.</summary>
    public LrcDocumentBuilder Clear()
    {
        ClearLines();
        ClearMetadata();
        return this;
    }

    /// <summary>Drop all lines (metadata kept).</summary>
    public LrcDocumentBuilder ClearLines()
    {
        _lines.Clear();
        return this;
    }

    /// <summary>Drop all metadata (lines kept).</summary>
    public LrcDocumentBuilder ClearMetadata()
    {
        _metadata = LrcMetadata.Empty;
        _rawTags.Clear();
        return this;
    }

    /// <summary>Add <paramref name="delta"/> to every line and word timestamp.
    /// Validates upfront — if any resulting timestamp would be negative, throws
    /// <see cref="ArgumentOutOfRangeException"/> and the builder is unchanged.
    /// To shift past zero, edit <see cref="LrcMetadata.Offset"/> instead.</summary>
    /// <param name="delta">Positive or negative offset.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A resulting timestamp would be negative,
    /// or the addition overflowed.</exception>
    public LrcDocumentBuilder ShiftAll(TimeSpan delta)
    {
        // Shift into a new list and swap it in only on success, so a failure leaves the builder unchanged.
        List<LrcLine> shifted;
        try
        {
            shifted = _lines.ConvertAll<LrcLine>(line => line switch
            {
                LrcPlainLine p => p with { Timestamp = p.Timestamp + delta },
                LrcEnhancedLine e => e with
                {
                    Timestamp = e.Timestamp + delta,
                    Words = EquatableArray.Create(e.Words.Select(w => w with { Timestamp = w.Timestamp + delta })),
                },
                _ => throw new UnreachableException("LrcLine hierarchy is sealed."),
            });
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(delta),
                "ShiftAll would produce a negative or overflowing timestamp. Adjust Metadata.Offset instead.");
        }

        _lines.Clear();
        _lines.AddRange(shifted);
        return this;
    }

    /// <summary>Remove the line at <paramref name="index"/> (insertion order).</summary>
    public LrcDocumentBuilder RemoveLineAt(int index)
    {
        _lines.RemoveAt(index);
        return this;
    }

    /// <summary>Remove every line for which <paramref name="predicate"/> returns true.</summary>
    public LrcDocumentBuilder RemoveLinesWhere(Func<LrcLine, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _lines.RemoveAll(predicate.Invoke);
        return this;
    }

    /// <summary>Replace the line at <paramref name="index"/> (insertion order).</summary>
    public LrcDocumentBuilder ReplaceLine(int index, LrcLine replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        _lines[index] = replacement;
        return this;
    }

    /// <summary>Materialize an immutable <see cref="LrcDocument"/>. Sorts by timestamp
    /// (stable: ties resolved by insertion order). Idempotent — does not mutate the builder.</summary>
    /// <returns>A fresh immutable document. Calling <see cref="Build"/> again after further
    /// fluent calls produces an updated document; the builder stays usable.</returns>
    public LrcDocument Build()
        => new()
        {
            Metadata = _metadata with { RawTags = ImmutableArray.CreateRange(_rawTags) },
            Lines = ImmutableCollectionsMarshal.AsImmutableArray(LrcLineStableSort.Sort(_lines, out _)),
        };
}
