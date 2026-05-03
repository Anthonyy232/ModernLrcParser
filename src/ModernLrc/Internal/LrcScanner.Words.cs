using System.Collections.Immutable;
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
            EmitPlainFanOut(contentToEol.ToString(), lineStamps);
            _cursor.Position += contentToEol.Length;
            _cursor.ConsumeLineTerminator();
            return;
        }

        // Text before the first '<' is attached as a word anchored at the first line timestamp.
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
            int openPosition = _cursor.Position;
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
            if (inside.IsEmpty || !LrcTimestamp.TryParseWithShape(inside, out var shape))
            {
                _diag.Emit(LrcDiagnosticSeverity.Error, LrcDiagnosticIds.InvalidEnhancedTimestamp,
                    openLine, openColumn, closeIdx - _cursor.Position + 2,
                    $"Invalid enhanced timestamp '<{inside.ToString()}>'.");
                if (_diag.StrictBail) return;

                if (words.Count > 0)
                {
                    _cursor.Position = openPosition;
                    int tailEol = _cursor.IndexOfLineEnd();
                    var tail = StripTrailingCr(_cursor.Slice(tailEol - _cursor.Position)).ToString();
                    var last = words[^1];
                    words[^1] = last with { Text = last.Text + tail };
                    _cursor.Position = tailEol;
                    break;
                }

                _cursor.Position = closeIdx + 1;
                continue;
            }
            if (shape.FractionLength > 3)
            {
                _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.TruncatedPrecision,
                    openLine, openColumn, inside.Length + 2,
                    $"Sub-millisecond precision in '<{inside.ToString()}>' was truncated.");
            }
            _cursor.Position = closeIdx + 1;

            int restEol = _cursor.IndexOfLineEnd();
            var rest = StripTrailingCr(_cursor.Slice(restEol - _cursor.Position));
            int nextLt = rest.IndexOf('<');
            ReadOnlySpan<char> wordText = nextLt < 0 ? rest : rest[..nextLt];
            words.Add(new LrcWord(shape.Value, wordText.ToString()));
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
            EmitPlainFanOut(verbatim.ToString(), lineStamps);
            _cursor.Position = originalPosition + verbatim.Length;
            _cursor.ConsumeLineTerminator();
            return;
        }

        var wordsArray = ImmutableArray.Create(CollectionsMarshal.AsSpan(words));
        if (lineStamps.Count == 1)
        {
            _lines.Add(new LrcEnhancedLine
            {
                Timestamp = lineStamps[0],
                Words = wordsArray,
                EffectiveVoice = _currentVoice,
            });
        }
        else
        {
            foreach (var ts in CollectionsMarshal.AsSpan(lineStamps))
            {
                _lines.Add(new LrcEnhancedLine
                {
                    Timestamp = ts,
                    Words = wordsArray,
                    EffectiveVoice = _currentVoice,
                });
            }
        }
        _cursor.ConsumeLineTerminator();
    }

    /// <summary>Emit one <see cref="LrcPlainLine"/> per timestamp in <paramref name="stamps"/>,
    /// each sharing the same <paramref name="text"/> reference. Single-timestamp lines (the
    /// dominant case) take a branch-free fast path that avoids loop scaffolding and the
    /// <see cref="List{T}"/> indexer.</summary>
    private void EmitPlainFanOut(string text, List<LrcTimestamp> stamps)
    {
        if (stamps.Count == 1)
        {
            _lines.Add(new LrcPlainLine
            {
                Timestamp = stamps[0],
                Text = text,
                EffectiveVoice = _currentVoice,
            });
            return;
        }

        var span = CollectionsMarshal.AsSpan(stamps);
        foreach (var ts in span)
        {
            _lines.Add(new LrcPlainLine
            {
                Timestamp = ts,
                Text = text,
                EffectiveVoice = _currentVoice,
            });
        }
    }
}
