namespace ModernLrc.Diagnostics;

/// <summary>Severity of a diagnostic emitted by the parser or writer.</summary>
public enum LrcDiagnosticSeverity
{
    /// <summary>Informational; never prevents successful parse/write.</summary>
    Info = 0,

    /// <summary>Recoverable concern.</summary>
    Warning = 1,

    /// <summary>Strictness=Strict throws on first Error; Tolerant collects.</summary>
    Error = 2,
}
