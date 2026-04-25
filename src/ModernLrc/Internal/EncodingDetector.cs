using System.Text;
using System.Text.Unicode;
using ModernLrc.Diagnostics;

namespace ModernLrc.Internal;

internal static class EncodingDetector
{
    /// <summary>Detect input encoding via BOM → caller override → UTF-8 validation → fallback.
    /// Returns the decoded string (or throws <see cref="LrcParseException"/> when encoding cannot be resolved).</summary>
    /// <remarks>BOM beats <see cref="LrcParseOptions.Encoding"/> intentionally: a BOM is a
    /// self-identifying marker and matches <see cref="System.IO.StreamReader"/>'s default.
    /// If you need caller <c>Encoding</c> to override a (possibly stray) BOM, strip the BOM
    /// from the byte array yourself before calling.</remarks>
    public static string Decode(ReadOnlySpan<byte> input, LrcParseOptions options, LrcDiagnosticEmitter? diag = null)
    {
        // 1) BOM check — UTF-8, UTF-16 LE, UTF-16 BE. See <remarks> on this method for precedence.
        if (input.Length >= 3 && input[0] == 0xEF && input[1] == 0xBB && input[2] == 0xBF)
            return Encoding.UTF8.GetString(input[3..]);
        if (input.Length >= 2 && input[0] == 0xFF && input[1] == 0xFE)
            return Encoding.Unicode.GetString(input[2..]);
        if (input.Length >= 2 && input[0] == 0xFE && input[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(input[2..]);

        // 2) Caller override — explicit encoding wins over auto-detection of unmarked input.
        if (options.Encoding is not null)
            return options.Encoding.GetString(input);

        // 3) UTF-8 validation via Utf8.IsValid — avoids exception-based control flow
        if (Utf8.IsValid(input))
            return Encoding.UTF8.GetString(input);

        // 4) Fallback encoding — if caller provided one, emit diagnostic and use it
        if (options.FallbackEncoding is null)
            throw new LrcParseException(
                "Encoding could not be detected. Set LrcParseOptions.Encoding to the file's actual encoding.");

        diag?.Emit(LrcDiagnosticSeverity.Error, LrcDiagnosticIds.EncodingFallback,
            line: 1, column: 1, length: 0,
            $"Encoding fallback to {options.FallbackEncoding.WebName} used.");

        return options.FallbackEncoding.GetString(input);
    }
}
