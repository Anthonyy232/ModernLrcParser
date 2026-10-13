using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Internal;

internal ref partial struct LrcScanner
{
    private LrcCursor _cursor;
    private readonly LrcDiagnosticEmitter _diag;

    /// <summary><see cref="long.MaxValue"/> when implausibility detection is disabled.</summary>
    private readonly long _implausibleTicks;

    private readonly List<LrcLine> _lines = new();
    private readonly List<LrcTag> _rawTags = new();
    // Scratch lists reused across lines; their contents are copied out before the next Clear().
    private readonly List<LrcTimestamp> _stampScratch = new(2);
    private readonly List<LrcWord> _wordScratch = new(4);

    private LrcMetadata _metadata = LrcMetadata.Empty;
    /// <summary>Bit per <see cref="LrcMetadataField"/> already set, for duplicate/conflict diagnostics.</summary>
    private int _seenFields;

    private LrcVoice _currentVoice = LrcVoice.Default;

    public LrcScanner(ReadOnlySpan<char> source, LrcParseOptions options, LrcDiagnosticEmitter emitter)
    {
        _cursor = new LrcCursor(source);
        _diag = emitter;
        _implausibleTicks = options.ImplausibleTimestampThreshold > TimeSpan.Zero
            ? options.ImplausibleTimestampThreshold.Ticks
            : long.MaxValue;
    }

    public LrcParseResult Run()
    {
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

        return new LrcParseResult
        {
            Document = new LrcDocument
            {
                Metadata = _metadata with { RawTags = ImmutableArray.CreateRange(_rawTags) },
                Lines = ImmutableCollectionsMarshal.AsImmutableArray(sorted),
            },
            Diagnostics = _diag.ToImmutableArray(),
        };
    }

    private void StripLanguagePrefixIfPresent()
    {
        // Pattern: 2 or 3 ASCII letters followed by "||" at the very start. Covers ISO 639-1
        // and ISO 639-2 codes without enumerating them.
        var start = _cursor.RemainingSpan;
        int letters = 0;
        while (letters < 3 && letters < start.Length && char.IsAsciiLetter(start[letters])) letters++;
        if (letters < 2 || !start[letters..].StartsWith("||", StringComparison.Ordinal)) return;

        int prefixLength = letters + 2;
        _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.Id3LanguagePrefixStripped,
            _cursor.Line, _cursor.Column, prefixLength,
            $"ID3 language prefix '{start[..prefixLength].ToString()}' stripped from start of input.");
        _cursor.Position += prefixLength;
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
