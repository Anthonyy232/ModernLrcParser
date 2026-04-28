using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Internal;

internal ref partial struct LrcScanner
{
    private void ScanLine()
    {
        // Skip empty lines.
        while (!_cursor.IsAtEnd && (_cursor.Peek() == '\r' || _cursor.Peek() == '\n'))
        {
            _cursor.ConsumeLineTerminator();
        }
        if (_cursor.IsAtEnd) return;

        // Reuse the per-scanner scratch list — avoids a List allocation per line.
        var stamps = _stampScratch;
        stamps.Clear();
        bool firstGroup = true;
        bool isMetadataLine = false;

        while (_cursor.Peek() == '[')
        {
            int openLine = _cursor.Line;
            int openColumn = _cursor.Column;
            int openBracketPosition = _cursor.Position; // saved for rollback when '[…]' becomes content text
            _cursor.Advance();

            char first = _cursor.Peek();
            if (firstGroup && IsIdTagFirstChar(first))
            {
                // IndexOfWithinLine bounds the scan to the current line — a stray ']' on a
                // later line cannot be silently absorbed into a multi-line tag value (the
                // LRC format is line-based; cross-line tags would also corrupt diag line
                // tracking on subsequent emissions).
                int closeIdx = _cursor.IndexOfWithinLine(']');
                if (closeIdx < 0)
                {
                    EmitUnclosedAndSkipLine(LrcDiagnosticIds.UnclosedTag, '[', openLine, openColumn);
                    return;
                }

                var tagSpan = _cursor.Slice(closeIdx - _cursor.Position);
                ParseIdTag(tagSpan, openLine, openColumn);
                _cursor.Position = closeIdx + 1;
                isMetadataLine = true;
                break; // metadata line — no further timestamps expected
            }

            // Try parse timestamp. Bounded to current line for the same reason as ID tags.
            int closeIdx2 = _cursor.IndexOfWithinLine(']');
            if (closeIdx2 < 0)
            {
                EmitUnclosedAndSkipLine(LrcDiagnosticIds.UnclosedTag, '[', openLine, openColumn);
                return;
            }

            var inside = _cursor.Slice(closeIdx2 - _cursor.Position);
            if (inside.IsEmpty)
            {
                _diag.Emit(LrcDiagnosticSeverity.Warning, LrcDiagnosticIds.EmptyTimestamp,
                    openLine, openColumn, 2, "Empty timestamp '[]'.");
                _cursor.Position = closeIdx2 + 1;
                firstGroup = false;
                continue;
            }

            if (LrcTimestamp.TryParseWithShape(inside, out var shape))
            {
                if (!shape.IsCanonical)
                {
                    _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.NonStandardTimestamp,
                        openLine, openColumn, inside.Length + 2,
                        $"Non-standard timestamp variant '[{inside.ToString()}]' accepted.");
                }
                if (shape.Value.Ticks > _implausibleTicks)
                {
                    _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.ImplausibleTimestamp,
                        openLine, openColumn, inside.Length + 2,
                        $"Timestamp '{shape.Value}' exceeds implausibility threshold.");
                }
                if (shape.FractionLength > 3)
                {
                    _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.TruncatedPrecision,
                        openLine, openColumn, inside.Length + 2,
                        $"Sub-millisecond precision in '[{inside.ToString()}]' was truncated.");
                }
                stamps.Add(shape.Value);
                _cursor.Position = closeIdx2 + 1;
            }
            else if (firstGroup)
            {
                // Could be an unknown-key ID tag if there's a colon AND the first char isn't a digit.
                // Digit-prefixed brackets that fail timestamp parsing are malformed timestamps,
                // not metadata tags.
                int colonIdx = inside.IndexOf(':');
                if (colonIdx > 0 && !char.IsAsciiDigit(inside[0]))
                {
                    ParseIdTag(inside, openLine, openColumn);
                    _cursor.Position = closeIdx2 + 1;
                    isMetadataLine = true;
                    break;
                }
                _diag.Emit(LrcDiagnosticSeverity.Error, LrcDiagnosticIds.InvalidTimestamp,
                    openLine, openColumn, closeIdx2 - openBracketPosition + 1,
                    $"'[{inside.ToString()}]' is not a valid timestamp.");
                _cursor.Position = closeIdx2 + 1;
            }
            else
            {
                // Tolerant: subsequent [...] that fails to parse → treat as content (LRC0091).
                _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.BracketedContentTolerance,
                    openLine, openColumn, closeIdx2 - openBracketPosition + 1,
                    "Bracketed group treated as content text.");
                // Roll back position to start of '[' so it becomes part of the line text.
                _cursor.Position = openBracketPosition;
                break;
            }
            firstGroup = false;

            if (_diag.StrictBail) return;
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

        // Voice marker: at least one timestamp consumed; check for "[MFD]: " prefix.
        ConsumeVoiceMarkerIfPresent();

        // One IndexOfAny scan classifies the line and locates its end at the same time:
        // the first of '\n', '\r', '<' tells us both whether this is enhanced (saw '<' first)
        // and where the plain-text branch ends.
        ReadOnlySpan<char> rest = _cursor.RemainingSpan;
        int firstSpecialCharIndex = rest.IndexOfAny('\n', '\r', '<');
        bool isEnhanced = firstSpecialCharIndex >= 0 && rest[firstSpecialCharIndex] == '<';

        if (isEnhanced)
        {
            ScanEnhancedWords(stamps);
        }
        else
        {
            int eolIdx = firstSpecialCharIndex < 0 ? _cursor.Length : _cursor.Position + firstSpecialCharIndex;
            var contentSlice = _cursor.Slice(eolIdx - _cursor.Position);
            var text = contentSlice.ToString();
            if (text.Length == 0)
            {
                _diag.Emit(LrcDiagnosticSeverity.Warning, LrcDiagnosticIds.TimestampWithoutText,
                    _cursor.Line, _cursor.Column, 0, "Timestamp without following text.");
            }
            EmitPlainFanOut(text, stamps);
            _cursor.Position += contentSlice.Length;
            _cursor.ConsumeLineTerminator();
        }
    }

    private static bool IsIdTagFirstChar(char c)
        => c is >= 'a' and <= 'z' || c is >= 'A' and <= 'Z' || c == '#';
}
