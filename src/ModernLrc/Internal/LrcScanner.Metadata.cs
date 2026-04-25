using System.Globalization;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Internal;

internal ref partial struct LrcScanner
{
    private void ParseIdTag(ReadOnlySpan<char> inside, int line, int column)
    {
        int colonIdx = inside.IndexOf(':');
        if (colonIdx <= 0)
        {
            _diag.Emit(LrcDiagnosticSeverity.Error, LrcDiagnosticIds.MalformedIdTag,
                line, column, inside.Length + 2, $"Malformed ID tag '[{inside.ToString()}]'.");
            return;
        }

        var keySpan = inside[..colonIdx];
        var valueSpan = inside[(colonIdx + 1)..];
        string value = valueSpan.ToString();

        // All known LRC keys are ≤ 6 chars (ti, ar, al, au, lr, by, re, tool, ve, length, offset).
        // For unusually long keys (> 32 chars), no known key can match — emit UnknownIdTag directly.
        if (keySpan.Length > 32)
        {
#pragma warning disable CA1308 // LRC ID tag keys are normalised to lowercase by spec
            string unknownLongKey = keySpan.ToString().ToLowerInvariant();
#pragma warning restore CA1308
            _rawTags.Add(new LrcTag(unknownLongKey, value));
            _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.UnknownIdTag,
                line, column, inside.Length + 2, $"Unknown ID tag '{unknownLongKey}' preserved as raw.");
            return;
        }

        // Lowercase into a stack buffer; the buffer scope is the same as keyLower's use below.
        Span<char> keyLowerBuf = stackalloc char[32];
#pragma warning disable CA1308 // LRC ID tag keys are normalised to lowercase by spec; ToUpperInvariant would break all switch cases
        int keyWritten = keySpan.ToLowerInvariant(keyLowerBuf);
#pragma warning restore CA1308
        var keyLower = keyLowerBuf[..keyWritten];

        // Switch on span — known keys use BCL interned string literals (zero allocation).
        // Each case stages the value into a private field; LrcMetadata is constructed once
        // at end-of-Run, avoiding the per-tag record allocation that `with` would incur.
        int length = inside.Length + 2;
        switch (keyLower)
        {
            case "ti":
                _rawTags.Add(new LrcTag("ti", value));
                HandleConflict("ti", _title, value, line, column, length);
                _title = value;
                break;

            case "ar":
                _rawTags.Add(new LrcTag("ar", value));
                HandleConflict("ar", _artist, value, line, column, length);
                _artist = value;
                break;

            case "al":
                _rawTags.Add(new LrcTag("al", value));
                HandleConflict("al", _album, value, line, column, length);
                _album = value;
                break;

            case "au":
                _rawTags.Add(new LrcTag("au", value));
                HandleConflict("au", _author, value, line, column, length);
                _author = value;
                break;

            case "lr":
                _rawTags.Add(new LrcTag("lr", value));
                HandleConflict("lr", _lyricist, value, line, column, length);
                _lyricist = value;
                break;

            case "by":
                _rawTags.Add(new LrcTag("by", value));
                HandleConflict("by", _createdBy, value, line, column, length);
                _createdBy = value;
                break;

            case "re":
                _rawTags.Add(new LrcTag("re", value));
                HandleConflict("re", _tool, value, line, column, length);
                _tool = value;
                break;

            case "tool":
                _rawTags.Add(new LrcTag("tool", value));
                HandleConflict("tool", _tool, value, line, column, length);
                _tool = value;
                break;

            case "ve":
                _rawTags.Add(new LrcTag("ve", value));
                HandleConflict("ve", _version, value, line, column, length);
                _version = value;
                break;

            case "length":
                _rawTags.Add(new LrcTag("length", value));
                if (LrcTimestamp.TryParse(value, CultureInfo.InvariantCulture, out var lengthTs))
                {
                    var newLength = lengthTs.ToTimeSpan();
                    if (_length is TimeSpan oldLength)
                        EmitMetadataConflict("length", oldLength == newLength, line, column, length);
                    _length = newLength;
                }
                else
                {
                    _diag.Emit(LrcDiagnosticSeverity.Warning, LrcDiagnosticIds.InvalidLength,
                        line, column, length, $"Invalid length value '{value}'.");
                }
                break;

            case "offset":
                _rawTags.Add(new LrcTag("offset", value));
                if (long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long ms))
                {
                    var newOffset = TimeSpan.FromMilliseconds(ms);
                    if (_offsetSet)
                        EmitMetadataConflict("offset", _offset == newOffset, line, column, length);
                    _offset = newOffset;
                    _offsetSet = true;
                }
                else
                {
                    _diag.Emit(LrcDiagnosticSeverity.Warning, LrcDiagnosticIds.InvalidOffset,
                        line, column, length, $"Invalid offset value '{value}'.");
                }
                break;

            default:
                // Unknown short key — materialize the lowercased span as a string.
                string unknownKey = keyLower.ToString();
                _rawTags.Add(new LrcTag(unknownKey, value));
                _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.UnknownIdTag,
                    line, column, length, $"Unknown ID tag '{unknownKey}' preserved as raw.");
                break;
        }
    }

    // Helper to deduplicate conflict-detection logic.
    private void HandleConflict(string key, string? oldValue, string newValue, int line, int column, int length)
    {
        if (oldValue is null) return;
        EmitMetadataConflict(key, string.Equals(oldValue, newValue, StringComparison.Ordinal), line, column, length);
    }

    private void EmitMetadataConflict(string key, bool isIdentical, int line, int column, int length)
    {
        var (severity, code) = isIdentical
            ? (LrcDiagnosticSeverity.Info, LrcDiagnosticIds.DuplicateMetadata)
            : (LrcDiagnosticSeverity.Warning, LrcDiagnosticIds.ConflictingMetadata);
        _diag.Emit(severity, code, line, column, length, $"Repeated metadata key '{key}'.");
    }

    private void ConsumeVoiceMarkerIfPresent()
    {
        // Skip leading whitespace before potential marker (only spaces and tabs).
        int savedPos = _cursor.Position;
        while (!_cursor.IsAtEnd && (_cursor.Peek() == ' ' || _cursor.Peek() == '\t'))
            _cursor.Advance();

        // Check for "X: " where X is M/F/D and a single ASCII space follows.
        if (_cursor.Position + 2 < _cursor.Length)
        {
            char marker = _cursor.Peek();
            char colon = _cursor.PeekAt(1);
            char space = _cursor.PeekAt(2);
            if (colon == ':' && space == ' ' && marker is 'M' or 'F' or 'D')
            {
                _currentVoice = marker switch
                {
                    'M' => LrcVoice.Male,
                    'F' => LrcVoice.Female,
                    'D' => LrcVoice.Duet,
                    _ => LrcVoice.Default,
                };
                _cursor.Position += 3;
                return;
            }
        }

        // Not a marker; restore position so leading whitespace is part of the text.
        _cursor.Position = savedPos;
    }
}
