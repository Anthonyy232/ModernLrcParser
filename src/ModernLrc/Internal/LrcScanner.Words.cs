using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Internal;

internal ref partial struct LrcScanner
{
    /// <summary>Drop a trailing CR if present. The line-end scan stops at the first of LF/CR,
    /// so a CRLF sequence yields a slice that ends in CR — we strip it so word text doesn't
    /// inherit a stray carriage return.</summary>
    private static ReadOnlySpan<char> StripTrailingCr(ReadOnlySpan<char> s)
        => s.Length > 0 && s[^1] == '\r' ? s[..^1] : s;

    private void ScanEnhancedWords(List<LrcTimestamp> lineStamps)
    {
        // Enhanced word grammar: [line-ts]<word-ts>text<word-ts>text...
        // Each word's Text includes verbatim slice from after '>' to next '<' (or line end).
        // Reuse per-scanner scratch list to avoid per-enhanced-line allocation.
        var words = _wordScratch;
        words.Clear();

        int eolIdx = _cursor.IndexOfLineEnd();

        // Capture content before attempting to parse words — used for plain-line fallback.
        int originalPosition = _cursor.Position;

        // Check for leading text before the first '<' (relative to current position).
        var contentToEol = StripTrailingCr(_cursor.Slice(eolIdx - _cursor.Position));

        int firstLt = contentToEol.IndexOf('<');

        if (firstLt < 0)
        {
            // No '<' at all — degenerate "enhanced" line (called from Lines.cs which saw a '<'
            // that disappeared after voice-marker consumption or trimming). Promote to plain line.
            _lines.Add(new LrcPlainLine
            {
                Timestamps = ImmutableArray.Create(CollectionsMarshal.AsSpan(lineStamps)),
                Text = contentToEol.ToString(),
                EffectiveVoice = _currentVoice,
            });
            _cursor.Position += contentToEol.Length;
            _cursor.ConsumeLineTerminator();
            return;
        }

        // Text before the first '<' is attached as a word anchored at the line timestamp.
        if (firstLt > 0)
        {
            var leading = contentToEol[..firstLt];
            words.Add(new LrcWord(lineStamps[0], leading.ToString()));
            _cursor.Position += firstLt;
        }

        // Snapshot count so we can detect "no valid <ts>word pairs found" after the loop.
        int wordsBeforeLoop = words.Count;

        while (!_cursor.IsAtEnd && _cursor.Peek() == '<')
        {
            int openLine = _cursor.Line;
            int openColumn = _cursor.Column;
            _cursor.Advance();

            // Bounded to current line: '<' must close before any newline. Same shape and
            // same recovery as the bracket-bounded path in ScanLine.
            int closeIdx = _cursor.IndexOfWithinLine('>');
            if (closeIdx < 0)
            {
                EmitUnclosedAndSkipLine(LrcDiagnosticIds.UnclosedEnhancedTimestamp, '<', openLine, openColumn);
                return;
            }

            var inside = _cursor.Slice(closeIdx - _cursor.Position);
            if (inside.IsEmpty || !LrcTimestamp.TryParse(inside, CultureInfo.InvariantCulture, out var ts))
            {
                _diag.Emit(LrcDiagnosticSeverity.Error, LrcDiagnosticIds.InvalidEnhancedTimestamp,
                    openLine, openColumn, closeIdx - _cursor.Position + 2,
                    $"Invalid enhanced timestamp '<{inside.ToString()}>'.");
                _cursor.Position = closeIdx + 1;
                continue;
            }
            // LRC0031: input had sub-millisecond precision that was truncated.
            // Reuses ScanTimestampShape so '.', ',', and colon-fraction separators all count.
            _ = ScanTimestampShape(inside, out int fractionLength);
            if (fractionLength > 3)
            {
                _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.TruncatedPrecision,
                    openLine, openColumn, inside.Length + 2,
                    $"Sub-millisecond precision in '<{inside.ToString()}>' was truncated.");
            }
            _cursor.Position = closeIdx + 1;

            // Word text = verbatim from here to next '<' or EOL.
            int restEol = _cursor.IndexOfLineEnd();
            var rest = StripTrailingCr(_cursor.Slice(restEol - _cursor.Position));
            int nextLt = rest.IndexOf('<');
            ReadOnlySpan<char> wordText = nextLt < 0 ? rest : rest[..nextLt];
            words.Add(new LrcWord(ts, wordText.ToString()));
            _cursor.Position += wordText.Length;
        }

        if (words.Count == wordsBeforeLoop)
        {
            _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.EmptyEnhancedWord,
                _cursor.Line, _cursor.Column, 0, "Enhanced line had no parseable words.");

            // Enhanced parse produced no valid <ts>word pairs — demote to plain line preserving verbatim content.
            // This is more useful than emitting LRC0007 + dropping the text.
            _cursor.Position = originalPosition;
            int fallbackEol = _cursor.IndexOfLineEnd();
            var verbatim = StripTrailingCr(_cursor.Slice(fallbackEol - _cursor.Position));
            _lines.Add(new LrcPlainLine
            {
                Timestamps = ImmutableArray.Create(CollectionsMarshal.AsSpan(lineStamps)),
                Text = verbatim.ToString(),
                EffectiveVoice = _currentVoice,
            });
            _cursor.Position = originalPosition + verbatim.Length;
            _cursor.ConsumeLineTerminator();
            return;
        }

        _lines.Add(new LrcEnhancedLine
        {
            Timestamps = ImmutableArray.Create(CollectionsMarshal.AsSpan(lineStamps)),
            Words = ImmutableArray.Create(CollectionsMarshal.AsSpan(words)),
            EffectiveVoice = _currentVoice,
        });
        _cursor.ConsumeLineTerminator();
    }
}
