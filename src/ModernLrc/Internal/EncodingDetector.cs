using System.Text;
using System.Text.Unicode;
using ModernLrc.Diagnostics;

namespace ModernLrc.Internal;

internal static class EncodingDetector
{
    private static readonly Encoding[] BomEncodings = [Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode];

    /// <summary>Detect input encoding via BOM → caller override → UTF-8 validation → fallback.
    /// Returns the decoded string (or throws <see cref="LrcParseException"/> when encoding cannot be resolved).</summary>
    /// <remarks>A BOM beats <see cref="LrcParseOptions.Encoding"/>, as in <see cref="System.IO.StreamReader"/>.</remarks>
    public static string Decode(ReadOnlySpan<byte> input, LrcParseOptions options, LrcDiagnosticEmitter diag)
    {
        foreach (var encoding in BomEncodings)
        {
            if (input.StartsWith(encoding.Preamble))
                return encoding.GetString(input[encoding.Preamble.Length..]);
        }
        if (options.Encoding is not null)
            return options.Encoding.GetString(input);
        if (Utf8.IsValid(input))
            return Encoding.UTF8.GetString(input);
        if (options.FallbackEncoding is null)
            throw new LrcParseException(
                "Encoding could not be detected. Set LrcParseOptions.Encoding to the file's actual encoding.");

        diag.Emit(LrcDiagnosticSeverity.Error, LrcDiagnosticIds.EncodingFallback,
            line: 1, column: 1, length: 0,
            $"Encoding fallback to {options.FallbackEncoding.WebName} used.");

        return options.FallbackEncoding.GetString(input);
    }
}
