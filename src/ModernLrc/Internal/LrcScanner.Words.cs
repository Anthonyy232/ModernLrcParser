using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Internal;

internal ref partial struct LrcScanner
{
    /// <summary>Parse <c>text?&lt;ts&gt;text&lt;ts&gt;text…</c> after the line timestamps. Each word's
    /// text runs verbatim to the next <c>&lt;</c>, so concatenating the words reproduces the line.</summary>
    private void ScanEnhancedWords(List<LrcTimestamp> lineStamps)
    {
        int start = _cursor.Position;
        int line = _cursor.Line;
        int column = _cursor.Column;
        var content = _cursor.Slice(_cursor.IndexOfLineEnd() - start);
        if (content is [.. var body, '\r']) content = body; // CRLF: the line end scan stopped at the CR

        var words = _wordScratch;
        words.Clear();
        bool anyTimed = false;

        // ScanLine only calls this after finding a '<' on the line. Leading text is anchored
        // at the line timestamp.
        int i = content.IndexOf('<');
        if (i > 0) words.Add(new LrcWord(lineStamps[0], content[..i].ToString()));

        while (i < content.Length && content[i] == '<')
        {
            int close = content[i..].IndexOf('>');
            if (close < 0)
            {
                EmitUnclosedAndSkipLine(LrcDiagnosticIds.UnclosedEnhancedTimestamp, '<', line, column + i);
                return;
            }

            var inside = content.Slice(i + 1, close - 1);
            int afterTag = i + close + 1;
            if (inside.IsEmpty || !LrcTimestamp.TryParseWithShape(inside, out var shape))
            {
                _diag.Emit(LrcDiagnosticSeverity.Error, LrcDiagnosticIds.InvalidEnhancedTimestamp,
                    line, column + i, close + 1, $"Invalid enhanced timestamp '<{inside.ToString()}>'.");
                if (_diag.StrictBail) return;

                if (words.Count > 0)
                {
                    // Keep the rest of the line as literal text on the previous word.
                    words[^1] = words[^1] with { Text = words[^1].Text + content[i..].ToString() };
                    break;
                }
                i = afterTag;
                continue;
            }

            if (shape.FractionLength > 3)
            {
                _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.TruncatedPrecision,
                    line, column + i, close + 1, $"Sub-millisecond precision in '<{inside.ToString()}>' was truncated.");
            }

            int next = content[afterTag..].IndexOf('<');
            next = next < 0 ? content.Length : afterTag + next;
            words.Add(new LrcWord(shape.Value, content[afterTag..next].ToString()));
            anyTimed = true;
            i = next;
        }

        if (anyTimed)
        {
            var shared = ImmutableArray.Create(CollectionsMarshal.AsSpan(words));
            foreach (var ts in CollectionsMarshal.AsSpan(lineStamps))
                _lines.Add(new LrcEnhancedLine { Timestamp = ts, Words = shared, EffectiveVoice = _currentVoice });
        }
        else
        {
            // No valid <ts>word pair: keep the text as a plain line rather than dropping it.
            _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.EmptyEnhancedWord,
                line, column + i, 0, "Enhanced line had no parseable words.");
            EmitPlainFanOut(content.ToString(), lineStamps);
        }

        _cursor.Position = start + content.Length;
        _cursor.ConsumeLineTerminator();
    }

    /// <summary>Emit one <see cref="LrcPlainLine"/> per timestamp, all sharing <paramref name="text"/>.</summary>
    private void EmitPlainFanOut(string text, List<LrcTimestamp> stamps)
    {
        foreach (var ts in CollectionsMarshal.AsSpan(stamps))
            _lines.Add(new LrcPlainLine { Timestamp = ts, Text = text, EffectiveVoice = _currentVoice });
    }
}
