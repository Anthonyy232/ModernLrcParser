using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Internal;

internal ref partial struct LrcScanner
{
    private void ParseIdTag(ReadOnlySpan<char> inside, int line, int column)
    {
        int length = inside.Length + 2;
        int colonIdx = inside.IndexOf(':');
        if (colonIdx <= 0)
        {
            _diag.Emit(LrcDiagnosticSeverity.Error, LrcDiagnosticIds.MalformedIdTag,
                line, column, length, $"Malformed ID tag '[{inside.ToString()}]'.");
            return;
        }

        string key = inside[..colonIdx].ToString().ToLowerInvariant();
        string value = inside[(colonIdx + 1)..].ToString();
        _rawTags.Add(new LrcTag(key, value));

        if (LrcMetadataFields.FromKey(key) is not { } field)
        {
            _diag.Emit(LrcDiagnosticSeverity.Info, LrcDiagnosticIds.UnknownIdTag,
                line, column, length, $"Unknown ID tag '{key}' preserved as raw.");
            return;
        }

        if (!LrcMetadataFields.TryParse(field, value, out var parsed))
        {
            string code = field == LrcMetadataField.Length ? LrcDiagnosticIds.InvalidLength : LrcDiagnosticIds.InvalidOffset;
            _diag.Emit(LrcDiagnosticSeverity.Warning, code, line, column, length, $"Invalid {key} value '{value}'.");
            return;
        }

        int bit = 1 << (int)field;
        if ((_seenFields & bit) != 0)
        {
            var (severity, code) = Equals(LrcMetadataFields.Get(_metadata, field), parsed)
                ? (LrcDiagnosticSeverity.Info, LrcDiagnosticIds.DuplicateMetadata)
                : (LrcDiagnosticSeverity.Warning, LrcDiagnosticIds.ConflictingMetadata);
            _diag.Emit(severity, code, line, column, length, $"Repeated metadata key '{key}'.");
        }
        _seenFields |= bit;
        _metadata = LrcMetadataFields.With(_metadata, field, parsed);
    }
}
