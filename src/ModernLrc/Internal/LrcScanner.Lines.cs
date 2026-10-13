using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Internal;

internal ref partial struct LrcScanner
{
    private void ScanLine()
    {
        while (!_cursor.IsAtEnd && (_cursor.Peek() == '\r' || _cursor.Peek() == '\n'))
        {
            _cursor.ConsumeLineTerminator();
        }
        if (_cursor.IsAtEnd) return;

        var stamps = _stampScratch;
        stamps.Clear();
        bool firstGroup = true;
        bool isMetadataLine = false;

        while (!_diag.StrictBail && _cursor.Peek() == '[')
        {
            int openLine = _cursor.Line;
            int openColumn = _cursor.Column;
            int openPosition = _cursor.Position;
            _cursor.Advance();

            // LRC is line-based: a ']' on a later line must not close this group.
            int closeIdx = _cursor.IndexOfWithinLine(']');
            if (closeIdx < 0)
            {
                EmitUnclosedAndSkipLine(LrcDiagnosticIds.UnclosedTag, '[', openLine, openColumn);
                return;
            }

            var inside = _cursor.Slice(closeIdx - _cursor.Position);
            int groupLength = inside.Length + 2;

            if (firstGroup && IsIdTag(inside))
            {
                ParseIdTag(inside, openLine, openColumn);
                _cursor.Position = closeIdx + 1;
                isMetadataLine = true;
                break;
            }

            if (inside.IsEmpty)
            {
                _diag.Emit(LrcDiagnosticSeverity.Warning, LrcDiagnosticIds.EmptyTimestamp,
                    openLine, openColumn, groupLength, "Empty timestamp '[]'.");
            }
            else if (LrcTimestamp.TryParseWithShape(inside, out var shape))
            {
                if (!shape.IsCanonical)
                {
                    _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.NonStandardTimestamp,
                        openLine, openColumn, groupLength,
                        $"Non-standard timestamp variant '[{inside.ToString()}]' accepted.");
                }
                if (shape.Value.Ticks > _implausibleTicks)
                {
                    _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.ImplausibleTimestamp,
                        openLine, openColumn, groupLength,
                        $"Timestamp '{shape.Value}' exceeds implausibility threshold.");
                }
                if (shape.FractionLength > 3)
                {
                    _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.TruncatedPrecision,
                        openLine, openColumn, groupLength,
                        $"Sub-millisecond precision in '[{inside.ToString()}]' was truncated.");
                }
                stamps.Add(shape.Value);
            }
            else if (firstGroup)
            {
                _diag.Emit(LrcDiagnosticSeverity.Error, LrcDiagnosticIds.InvalidTimestamp,
                    openLine, openColumn, groupLength, $"'[{inside.ToString()}]' is not a valid timestamp.");
            }
            else
            {
                // After a timestamp, an unparseable group is part of the lyric text (LRC0091).
                _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.BracketedContentTolerance,
                    openLine, openColumn, groupLength, "Bracketed group treated as content text.");
                _cursor.Position = openPosition;
                break;
            }

            _cursor.Position = closeIdx + 1;
            firstGroup = false;
        }

        if (_diag.StrictBail) return;

        if (isMetadataLine || stamps.Count == 0)
        {
            // Skip remainder of the line (metadata) or drop free text (LRC0050).
            int contentStart = _cursor.Position;
            int contentLine = _cursor.Line;
            int contentColumn = _cursor.Column;
            _cursor.SkipToLineEnd();
            if (!isMetadataLine && _cursor.Position > contentStart)
            {
                _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.DroppedUntimedText,
                    contentLine, contentColumn, _cursor.Position - contentStart,
                    "Free text without timestamp dropped.");
            }
            _cursor.ConsumeLineTerminator();
            return;
        }
        ConsumeVoiceMarkerIfPresent();

        // One IndexOfAny scan classifies the line and locates its end at the same time:
        // the first of '\n', '\r', '<' tells us both whether this is enhanced (saw '<' first)
        // and where the plain-text branch ends.
        var rest = _cursor.RemainingSpan;
        int special = rest.IndexOfAny('\n', '\r', '<');
        if (special >= 0 && rest[special] == '<')
        {
            ScanEnhancedWords(stamps);
            return;
        }

        var text = (special < 0 ? rest : rest[..special]).ToString();
        if (text.Length == 0)
        {
            _diag.Emit(LrcDiagnosticSeverity.Warning, LrcDiagnosticIds.TimestampWithoutText,
                _cursor.Line, _cursor.Column, 0, "Timestamp without following text.");
        }
        EmitPlainFanOut(text, stamps);
        _cursor.Position += text.Length;
        _cursor.ConsumeLineTerminator();
    }

    /// <summary>A bracket group that starts like an ID tag (<c>[ti:…]</c>, <c>[#…]</c>). A
    /// digit-led group is always treated as a (possibly malformed) timestamp.</summary>
    private static bool IsIdTag(ReadOnlySpan<char> inside)
        => inside is [var c, ..]
           && (char.IsAsciiLetter(c) || c == '#' || (!char.IsAsciiDigit(c) && inside.IndexOf(':') > 0));

    /// <summary>Consume a Walaoke <c>M: </c> / <c>F: </c> / <c>D: </c> marker (after optional
    /// spaces or tabs) and make it the current voice.</summary>
    private void ConsumeVoiceMarkerIfPresent()
    {
        var rest = _cursor.RemainingSpan.TrimStart(" \t");
        if (rest is not ['M' or 'F' or 'D', ':', ' ', ..]) return;

        _currentVoice = rest[0] switch
        {
            'M' => LrcVoice.Male,
            'F' => LrcVoice.Female,
            _ => LrcVoice.Duet,
        };
        _cursor.Position = _cursor.Length - rest.Length + 3;
    }
}
