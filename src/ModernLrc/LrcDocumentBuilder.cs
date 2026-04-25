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

    private string? _title;
    private string? _artist;
    private string? _album;
    private string? _author;
    private string? _lyricist;
    private string? _createdBy;
    private string? _tool;
    private string? _version;
    private TimeSpan? _length;
    private TimeSpan _offset;

    /// <summary>Empty builder.</summary>
    public LrcDocumentBuilder() { }

    /// <summary>Seed from an existing document (shallow copy — records are immutable so sharing is safe).</summary>
    public LrcDocumentBuilder(LrcDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _title = source.Metadata.Title;
        _artist = source.Metadata.Artist;
        _album = source.Metadata.Album;
        _author = source.Metadata.Author;
        _lyricist = source.Metadata.Lyricist;
        _createdBy = source.Metadata.CreatedBy;
        _tool = source.Metadata.Tool;
        _version = source.Metadata.Version;
        _length = source.Metadata.Length;
        _offset = source.Metadata.Offset;
        _rawTags.AddRange(source.Metadata.RawTags);
        _lines.AddRange(source.Lines);
    }

    /// <summary>Number of lines currently held (in insertion order).</summary>
    public int LineCount => _lines.Count;

    /// <summary>Get a line in insertion order. Use <see cref="Build"/> to retrieve sorted output.</summary>
    public LrcLine GetLineAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _lines.Count);
        return _lines[index];
    }

    // ----- Metadata fluent setters -----

    /// <summary>Set or clear the title (<c>ti</c>).</summary>
    public LrcDocumentBuilder WithTitle(string? value) { _title = value; return this; }

    /// <summary>Set or clear the artist (<c>ar</c>).</summary>
    public LrcDocumentBuilder WithArtist(string? value) { _artist = value; return this; }

    /// <summary>Set or clear the album (<c>al</c>).</summary>
    public LrcDocumentBuilder WithAlbum(string? value) { _album = value; return this; }

    /// <summary>Set or clear the author (<c>au</c>).</summary>
    public LrcDocumentBuilder WithAuthor(string? value) { _author = value; return this; }

    /// <summary>Set or clear the lyricist (<c>lr</c>).</summary>
    public LrcDocumentBuilder WithLyricist(string? value) { _lyricist = value; return this; }

    /// <summary>Set or clear the created-by attribution (<c>by</c>).</summary>
    public LrcDocumentBuilder WithCreatedBy(string? value) { _createdBy = value; return this; }

    /// <summary>Set or clear the tool (<c>re</c> / <c>tool</c>).</summary>
    public LrcDocumentBuilder WithTool(string? value) { _tool = value; return this; }

    /// <summary>Set or clear the version (<c>ve</c>).</summary>
    public LrcDocumentBuilder WithVersion(string? value) { _version = value; return this; }

    /// <summary>Set or clear the track length (<c>length:mm:ss</c>).</summary>
    public LrcDocumentBuilder WithLength(TimeSpan? value) { _length = value; return this; }

    /// <summary>Set the document offset (<c>offset:±N</c> ms).</summary>
    public LrcDocumentBuilder WithOffset(TimeSpan value) { _offset = value; return this; }

    /// <summary>Append a raw tag entry (preserves insertion order).
    /// <paramref name="key"/> must be non-empty (whitespace-only is rejected — it would round-trip
    /// to <c>[ :value]</c> which the parser cannot read back).</summary>
    public LrcDocumentBuilder WithRawTag(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _rawTags.Add(new LrcTag(key, value));
        return this;
    }

    /// <summary>Remove every raw tag whose key matches (ordinal).</summary>
    public LrcDocumentBuilder RemoveRawTag(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _rawTags.RemoveAll(t => string.Equals(t.Key, key, StringComparison.Ordinal));
        return this;
    }

    // ----- Plain line addition -----

    /// <summary>Add a plain line whose timestamp is parsed from <paramref name="timestamp"/>.</summary>
    public LrcDocumentBuilder AddLine(string timestamp, string text, LrcVoice voice = LrcVoice.Default)
    {
        ArgumentNullException.ThrowIfNull(timestamp);
        ArgumentNullException.ThrowIfNull(text);
        return AddLine(LrcTimestamp.Parse(timestamp, CultureInfo.InvariantCulture), text, voice);
    }

    /// <summary>Add a plain line at <paramref name="timestamp"/>.</summary>
    public LrcDocumentBuilder AddLine(LrcTimestamp timestamp, string text, LrcVoice voice = LrcVoice.Default)
    {
        ArgumentNullException.ThrowIfNull(text);
        return AddLineCore([timestamp], text, voice);
    }

    /// <summary>Add a plain line at <paramref name="timestamp"/> (TimeSpan overload).</summary>
    public LrcDocumentBuilder AddLine(TimeSpan timestamp, string text, LrcVoice voice = LrcVoice.Default)
    {
        ArgumentNullException.ThrowIfNull(text);
        return AddLine(LrcTimestamp.FromTimeSpan(timestamp), text, voice);
    }

    /// <summary>Add a plain line carrying multiple timestamps.</summary>
    public LrcDocumentBuilder AddLine(ReadOnlySpan<LrcTimestamp> timestamps, string text, LrcVoice voice = LrcVoice.Default)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (timestamps.IsEmpty) throw new ArgumentException("At least one timestamp is required.", nameof(timestamps));
        return AddLineCore(timestamps, text, voice);
    }

    private LrcDocumentBuilder AddLineCore(ReadOnlySpan<LrcTimestamp> timestamps, string text, LrcVoice voice)
    {
        _lines.Add(new LrcPlainLine
        {
            Timestamps = EquatableArray.Create(timestamps),
            Text = text,
            EffectiveVoice = voice,
        });
        return this;
    }

    // ----- Enhanced line addition -----

    /// <summary>Add an enhanced (word-timed) line at a single timestamp.</summary>
    public LrcDocumentBuilder AddEnhancedLine(LrcTimestamp lineTimestamp, ReadOnlySpan<LrcWord> words, LrcVoice voice = LrcVoice.Default)
        => AddEnhancedLineCore([lineTimestamp], EquatableArray.Create(words), voice);

    /// <summary>Add an enhanced (word-timed) line from a tuple sequence.</summary>
    public LrcDocumentBuilder AddEnhancedLine(LrcTimestamp lineTimestamp, IEnumerable<(LrcTimestamp Time, string Text)> words, LrcVoice voice = LrcVoice.Default)
    {
        ArgumentNullException.ThrowIfNull(words);
        return AddEnhancedLineCore([lineTimestamp],
            EquatableArray.Create(words.Select(static w => new LrcWord(w.Time, w.Text))), voice);
    }

    /// <summary>Add an enhanced (word-timed) line carrying multiple top-level timestamps.</summary>
    public LrcDocumentBuilder AddEnhancedLine(ReadOnlySpan<LrcTimestamp> lineTimestamps, ReadOnlySpan<LrcWord> words, LrcVoice voice = LrcVoice.Default)
    {
        if (lineTimestamps.IsEmpty)
            throw new ArgumentException("At least one timestamp is required.", nameof(lineTimestamps));
        return AddEnhancedLineCore(lineTimestamps, EquatableArray.Create(words), voice);
    }

    /// <summary>Add an enhanced (word-timed) line with multiple top-level timestamps from a tuple sequence.</summary>
    public LrcDocumentBuilder AddEnhancedLine(ReadOnlySpan<LrcTimestamp> lineTimestamps, IEnumerable<(LrcTimestamp Time, string Text)> words, LrcVoice voice = LrcVoice.Default)
    {
        if (lineTimestamps.IsEmpty)
            throw new ArgumentException("At least one timestamp is required.", nameof(lineTimestamps));
        ArgumentNullException.ThrowIfNull(words);
        return AddEnhancedLineCore(lineTimestamps,
            EquatableArray.Create(words.Select(static w => new LrcWord(w.Time, w.Text))), voice);
    }

    private LrcDocumentBuilder AddEnhancedLineCore(ReadOnlySpan<LrcTimestamp> timestamps, EquatableArray<LrcWord> words, LrcVoice voice)
    {
        // Single chokepoint for null-Text validation. Catches both default(LrcWord) from span
        // callers and any null tuple-Text from the IEnumerable path.
        foreach (var w in words.AsSpan())
        {
            if (w.Text is null)
                throw new ArgumentException("LrcWord.Text cannot be null.", nameof(words));
        }
        _lines.Add(new LrcEnhancedLine
        {
            Timestamps = EquatableArray.Create(timestamps),
            Words = words,
            EffectiveVoice = voice,
        });
        return this;
    }

    /// <summary>Append a fully-constructed <see cref="LrcLine"/> (reference held; records are immutable).
    /// Throws <see cref="ArgumentException"/> if <paramref name="line"/>'s <c>Timestamps</c> collection is empty.</summary>
    public LrcDocumentBuilder AddLine(LrcLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (line.Timestamps.Count == 0)
            throw new ArgumentException("LrcLine must carry at least one timestamp.", nameof(line));
        _lines.Add(line);
        return this;
    }

    /// <summary>Append every line from a sequence (each must satisfy the ≥ 1 timestamp invariant).</summary>
    public LrcDocumentBuilder AddLines(IEnumerable<LrcLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        foreach (var line in lines)
            AddLine(line);
        return this;
    }

    // ----- Manipulation -----

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
        _title = _artist = _album = _author = _lyricist = _createdBy = _tool = _version = null;
        _length = null;
        _offset = TimeSpan.Zero;
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
        // Pass 1: validate non-negative result for every timestamp.
        // Convert overflow (e.g., delta = TimeSpan.MinValue) into the same
        // ArgumentOutOfRangeException contract for callers.
        try
        {
            foreach (var line in _lines)
            {
                foreach (var t in line.Timestamps)
                    if (checked(t.Ticks + delta.Ticks) < 0)
                        throw new ArgumentOutOfRangeException(nameof(delta),
                            "ShiftAll would produce a negative timestamp. Adjust Metadata.Offset instead.");
                if (line is LrcEnhancedLine enhanced)
                {
                    foreach (var word in enhanced.Words)
                        if (checked(word.Timestamp.Ticks + delta.Ticks) < 0)
                            throw new ArgumentOutOfRangeException(nameof(delta),
                                "ShiftAll would produce a negative word timestamp. Adjust Metadata.Offset instead.");
                }
            }
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(delta),
                "ShiftAll delta produced an arithmetic overflow.");
        }

        // Pass 2: rebuild every line with shifted values.
        for (int i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            var shiftedStamps = ShiftTimestamps(line.Timestamps, delta);
            _lines[i] = line switch
            {
                LrcPlainLine p => p with { Timestamps = shiftedStamps },
                LrcEnhancedLine e => e with
                {
                    Timestamps = shiftedStamps,
                    Words = ShiftWords(e.Words, delta),
                },
#pragma warning disable CS8509
                // Justification: LrcLine hierarchy is sealed (private protected constructor);
                // LrcPlainLine and LrcEnhancedLine are the only two concrete types.
                _ => throw new UnreachableException("LrcLine hierarchy is sealed."),
#pragma warning restore CS8509
            };
        }
        return this;
    }

    private static EquatableArray<LrcTimestamp> ShiftTimestamps(EquatableArray<LrcTimestamp> stamps, TimeSpan delta)
    {
        var arr = new LrcTimestamp[stamps.Count];
        for (int i = 0; i < stamps.Count; i++)
            arr[i] = stamps[i] + delta;
        return ImmutableArray.Create(arr);
    }

    private static EquatableArray<LrcWord> ShiftWords(EquatableArray<LrcWord> words, TimeSpan delta)
    {
        var arr = new LrcWord[words.Count];
        for (int i = 0; i < words.Count; i++)
            arr[i] = words[i] with { Timestamp = words[i].Timestamp + delta };
        return ImmutableArray.Create(arr);
    }

    /// <summary>Remove the line at <paramref name="index"/> (insertion order).</summary>
    public LrcDocumentBuilder RemoveLineAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _lines.Count);
        _lines.RemoveAt(index);
        return this;
    }

    /// <summary>Remove every line for which <paramref name="predicate"/> returns true.</summary>
    public LrcDocumentBuilder RemoveLinesWhere(Func<LrcLine, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _lines.RemoveAll(l => predicate(l));
        return this;
    }

    /// <summary>Replace the line at <paramref name="index"/> (insertion order).</summary>
    public LrcDocumentBuilder ReplaceLine(int index, LrcLine replacement)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _lines.Count);
        ArgumentNullException.ThrowIfNull(replacement);
        if (replacement.Timestamps.Count == 0)
            throw new ArgumentException("LrcLine must carry at least one timestamp.", nameof(replacement));
        _lines[index] = replacement;
        return this;
    }

    /// <summary>Materialize an immutable <see cref="LrcDocument"/>. Sorts by first timestamp
    /// (stable: ties resolved by insertion order). Idempotent — does not mutate the builder.</summary>
    /// <returns>A fresh immutable document. Calling <see cref="Build"/> again after further
    /// fluent calls produces an updated document; the builder stays usable.</returns>
    /// <remarks>The sort takes a fast O(N) path when lines were added in ascending timestamp
    /// order (the common case); otherwise an indexed stable sort is used.</remarks>
    public LrcDocument Build()
    {
        var metadata = new LrcMetadata
        {
            Title = _title,
            Artist = _artist,
            Album = _album,
            Author = _author,
            Lyricist = _lyricist,
            CreatedBy = _createdBy,
            Tool = _tool,
            Version = _version,
            Length = _length,
            Offset = _offset,
            RawTags = ImmutableArray.CreateRange(_rawTags),
        };

        var sorted = LrcLineStableSort.Sort(_lines, out _);

        return new LrcDocument
        {
            Metadata = metadata,
            Lines = ImmutableCollectionsMarshal.AsImmutableArray(sorted),
        };
    }
}
