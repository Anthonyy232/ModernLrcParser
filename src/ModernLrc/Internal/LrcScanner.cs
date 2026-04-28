using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Internal;

#pragma warning disable CA1849 // Call async methods when in an async method — not applicable; ref struct cannot be async
internal ref partial struct LrcScanner
{
    private LrcCursor _cursor;
    private readonly LrcDiagnosticEmitter _diag;

    /// <summary><see cref="long.MaxValue"/> when implausibility detection is disabled —
    /// every timestamp then trivially fails the per-line compare without branching out.</summary>
    private readonly long _implausibleTicks;

    private readonly List<LrcLine> _lines;
    private readonly List<LrcTag> _rawTags = new();
    // Scratch lists reused across ScanLine / ScanEnhancedWords calls — Clear() between lines
    // instead of allocating a fresh List per line. ImmutableArray.CreateRange copies out, so
    // it's safe to reuse the same backing list for the next line.
    private readonly List<LrcTimestamp> _stampScratch = new(2);
    private readonly List<LrcWord> _wordScratch = new(4);

    // Strongly-typed metadata staged as fields. The previous `_typedMetadata with { … }` chain
    // allocated a fresh LrcMetadata per ID tag; field mutation is allocation-free, and the
    // single LrcMetadata is constructed once at end-of-Run.
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
    private bool _offsetSet;

    private LrcVoice _currentVoice = LrcVoice.Default;

    /// <summary>Initialises the scanner with a source span, options, and a pre-constructed emitter.</summary>
    public LrcScanner(ReadOnlySpan<char> source, LrcParseOptions options, LrcDiagnosticEmitter emitter)
    {
        _cursor = new LrcCursor(source);
        _diag = emitter;

        _implausibleTicks = options.ImplausibleTimestampThreshold > TimeSpan.Zero
            ? options.ImplausibleTimestampThreshold.Ticks
            : long.MaxValue;

        // Length heuristic: ~30 chars/line is typical; floor at 4 to match List<T>'s default growth step.
        int estimatedLines = source.Length switch
        {
            < 64 => 4,
            < 4096 => source.Length / 24,
            _ => source.Length / 30,
        };
        _lines = new List<LrcLine>(estimatedLines);
    }

    /// <summary>Run the scanner over the entire source and return a <see cref="LrcParseResult"/>.</summary>
    public LrcParseResult Run()
    {
        // ID3-style language prefix (e.g., "eng||", "jpn||") — common in streaming-service
        // LRC exports. Strip if present.
        StripLanguagePrefixIfPresent();

        while (!_cursor.IsAtEnd && !_diag.StrictBail)
        {
            ScanLine();
        }

        var sorted = LrcLineStableSort.Sort(_lines, out bool reordered);
        if (reordered)
        {
            _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.LinesReordered,
                line: 1, column: 1, length: 0,
                "Lines were reordered to maintain ascending-by-timestamp guarantee.");
        }

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

        return new LrcParseResult
        {
            Document = new LrcDocument
            {
                Metadata = metadata,
                Lines = ImmutableCollectionsMarshal.AsImmutableArray(sorted),
            },
            Diagnostics = _diag.ToImmutableArray(),
        };
    }

    private void StripLanguagePrefixIfPresent()
    {
        // Pattern: 2 or 3 ASCII letters followed by "||" at the very start.
        // Covers ISO 639-1 (en, ja, zh, ko, es, fr, de, ru, …) and ISO 639-2
        // (eng, jpn, chi, kor, spa, fre/fra, ger/deu, rus, …) without enumerating codes.
        // Try the 3-letter form first so "eng||" doesn't get partial-matched as 2-letter "en" + "g||".
        if (_cursor.Position + 4 > _cursor.Length) return;

        char a = _cursor.PeekAt(0);
        char b = _cursor.PeekAt(1);
        if (!char.IsAsciiLetter(a) || !char.IsAsciiLetter(b)) return;

        // Try 3-letter form: 3 letters + "||" (5 chars total).
        if (_cursor.Position + 5 <= _cursor.Length)
        {
            char c = _cursor.PeekAt(2);
            if (char.IsAsciiLetter(c) && _cursor.PeekAt(3) == '|' && _cursor.PeekAt(4) == '|')
            {
                int prefixLine = _cursor.Line;
                int prefixColumn = _cursor.Column;
                for (int i = 0; i < 5; i++) _cursor.Advance();
                _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.Id3LanguagePrefixStripped,
                    prefixLine, prefixColumn, 5,
                    $"ID3 language prefix '{a}{b}{c}||' stripped from start of input.");
                return;
            }
        }

        // Fall back to 2-letter form: 2 letters + "||" (4 chars total).
        if (_cursor.PeekAt(2) == '|' && _cursor.PeekAt(3) == '|')
        {
            int prefixLine = _cursor.Line;
            int prefixColumn = _cursor.Column;
            for (int i = 0; i < 4; i++) _cursor.Advance();
            _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.Id3LanguagePrefixStripped,
                prefixLine, prefixColumn, 4,
                $"ID3 language prefix '{a}{b}||' stripped from start of input.");
        }
    }

    /// <summary>Centralised emission for an unclosed opening character (<c>[</c> for tags,
    /// <c>&lt;</c> for enhanced word timestamps). Same recovery in both: skip the rest of the line
    /// so the next iteration can attempt the next line cleanly.</summary>
    private void EmitUnclosedAndSkipLine(string code, char opener, int openLine, int openColumn)
    {
        _diag.Emit(LrcDiagnosticSeverity.Error, code,
            openLine, openColumn, 1, $"Unclosed '{opener}'.");
        _cursor.SkipToLineEnd();
        _cursor.ConsumeLineTerminator();
    }
}
