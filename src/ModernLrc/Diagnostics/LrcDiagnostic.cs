namespace ModernLrc.Diagnostics;

/// <summary>A single diagnostic emitted during parsing or writing.
/// <see cref="Line"/> is 1-based; <see cref="Column"/> is 1-based; <see cref="Length"/>
/// covers the offending source span (in chars). All locations refer to the original
/// input/output, not any normalized form.</summary>
public sealed record LrcDiagnostic
{
    /// <summary>Severity (Info / Warning / Error).</summary>
    public required LrcDiagnosticSeverity Severity { get; init; }

    /// <summary>Stable identifier from <see cref="LrcDiagnosticIds"/>.</summary>
    public required string Code { get; init; }

    /// <summary>1-based source line number.</summary>
    public required int Line { get; init; }

    /// <summary>1-based column number.</summary>
    public required int Column { get; init; }

    /// <summary>Length of the offending span, in chars.</summary>
    public required int Length { get; init; }

    /// <summary>Human-readable description.</summary>
    public required string Message { get; init; }
}
