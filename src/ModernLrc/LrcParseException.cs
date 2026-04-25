using ModernLrc.Diagnostics;

namespace ModernLrc;

/// <summary>Thrown for catastrophic parse failures: encoding-detect failure, or a
/// Strict-mode error on the first Error-severity diagnostic. <see cref="PartialResult"/>
/// is populated for content failures; null for encoding/IO failures. Infrastructure
/// errors (<see cref="System.IO.IOException"/>, <see cref="UnauthorizedAccessException"/>,
/// <see cref="OperationCanceledException"/>, etc.) are NOT wrapped — they propagate raw.</summary>
/// <example>
/// <code>
/// var options = new LrcParseOptions { Strictness = LrcStrictness.Strict };
/// try
/// {
///     var result = LrcParser.Parse(text, options);
/// }
/// catch (LrcParseException ex)
/// {
///     Console.WriteLine($"Parse failed: {ex.FirstError?.Code} at L{ex.FirstError?.Line}");
///     // ex.PartialResult holds whatever was parsed before the failure
///     // ex.FilePath is set when raised by ParseFile / ParseFileAsync
/// }
/// </code>
/// </example>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design", "CA1032:Implement standard exception constructors",
    Justification = "Spec deliberately omits the inner-exception overload; only message-only and rich constructors are exposed by design.")]
public sealed class LrcParseException : Exception
{
    /// <summary>The partial result built up to the failure point, if any.</summary>
    public LrcParseResult? PartialResult { get; }

    /// <summary>The first Error-severity diagnostic that triggered the throw, if any.</summary>
    public LrcDiagnostic? FirstError { get; }

    /// <summary>File path when raised by <c>ParseFile*</c>; null otherwise.</summary>
    public string? FilePath { get; }

    /// <summary>Message-only constructor (encoding-detect failure path).</summary>
    public LrcParseException(string message)
        : base(message) { }

    /// <summary>Rich constructor used by Strict-mode failures.</summary>
    public LrcParseException(
        string message,
        LrcParseResult? partialResult,
        LrcDiagnostic? firstError,
        string? filePath)
        : base(message)
    {
        PartialResult = partialResult;
        FirstError = firstError;
        FilePath = filePath;
    }
}
