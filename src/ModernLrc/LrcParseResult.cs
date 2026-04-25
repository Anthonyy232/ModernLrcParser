using System.Collections.Immutable;
using ModernLrc.Diagnostics;

namespace ModernLrc;

/// <summary>Outcome of a parse operation: the document built (possibly partial under
/// strict mode failure) and every diagnostic emitted in source order.</summary>
/// <example>
/// <code>
/// var result = LrcParser.Parse(text);
/// if (result.HasErrors)
///     foreach (var d in result.Diagnostics)
///         if (d.Severity == LrcDiagnosticSeverity.Error)
///             Console.WriteLine($"{d.Code} at L{d.Line}:C{d.Column} — {d.Message}");
/// // Document is always non-null; may be partial when strict-mode failed mid-parse.
/// var doc = result.Document;
/// </code>
/// </example>
public sealed record LrcParseResult
{
    /// <summary>Built document. Always non-null; may be partial when a Strict-mode parse
    /// fails on the first Error-severity diagnostic.</summary>
    public required LrcDocument Document { get; init; }

    /// <summary>All diagnostics emitted, in source order.</summary>
    public required ImmutableArray<LrcDiagnostic> Diagnostics { get; init; }

    /// <summary>True if any diagnostic has <see cref="LrcDiagnosticSeverity.Error"/> severity.</summary>
    public bool HasErrors
    {
        get
        {
            foreach (var d in Diagnostics)
                if (d.Severity == LrcDiagnosticSeverity.Error) return true;
            return false;
        }
    }

    /// <summary>True if any diagnostic has <see cref="LrcDiagnosticSeverity.Warning"/> severity.</summary>
    public bool HasWarnings
    {
        get
        {
            foreach (var d in Diagnostics)
                if (d.Severity == LrcDiagnosticSeverity.Warning) return true;
            return false;
        }
    }
}
