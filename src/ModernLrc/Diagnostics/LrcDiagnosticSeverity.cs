namespace ModernLrc.Diagnostics;

/// <summary>Severity of a diagnostic emitted by the parser or writer.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design", "CA1028:Enum storage should be Int32",
    Justification = "byte is intentional — diagnostics are emitted in tight loops; saving 3 bytes per occurrence matters at scale.")]
public enum LrcDiagnosticSeverity : byte
{
    /// <summary>Informational; never prevents successful parse/write.</summary>
    Info = 0,

    /// <summary>Recoverable concern.</summary>
    Warning = 1,

    /// <summary>Strictness=Strict throws on first Error; Tolerant collects.</summary>
    Error = 2,
}
