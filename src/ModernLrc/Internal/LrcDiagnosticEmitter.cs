using System.Collections.Immutable;
using ModernLrc.Diagnostics;

namespace ModernLrc.Internal;

/// <summary>Collects diagnostics during parsing. Respects the <see cref="LrcParseOptions.MaxDiagnostics"/>
/// cap (emits <c>LRC0099</c> once when the cap is hit). In Strict mode, the first Error-severity
/// diagnostic causes the parser to bail and the entry point to throw <see cref="LrcParseException"/>.</summary>
internal sealed class LrcDiagnosticEmitter
{
    private readonly List<LrcDiagnostic> _items = new();
    private readonly int _maxDiagnostics;
    private bool _capReached;

    /// <summary>The first Error-severity diagnostic emitted, even if the cap suppressed it.</summary>
    public LrcDiagnostic? FirstError { get; private set; }

    public bool IsStrict { get; }

    /// <summary>True after a Strict-mode parse has hit its first Error and unwinding should begin.</summary>
    public bool StrictBail => IsStrict && FirstError is not null;

    public LrcDiagnosticEmitter(LrcParseOptions options)
    {
        _maxDiagnostics = options.MaxDiagnostics;
        IsStrict = options.Strictness == LrcStrictness.Strict;
    }

    public void Emit(LrcDiagnosticSeverity severity, string code, int line, int column, int length, string message)
    {
        var diag = new LrcDiagnostic
        {
            Severity = severity,
            Code = code,
            Line = line,
            Column = column,
            Length = length,
            Message = message,
        };
        if (severity == LrcDiagnosticSeverity.Error)
            FirstError ??= diag;

        if (_items.Count < _maxDiagnostics)
        {
            _items.Add(diag);
        }
        else if (!_capReached)
        {
            _capReached = true;
            // MaxDiagnostics == 0 means total silence, including the cap notice itself.
            if (_maxDiagnostics > 0)
            {
                _items.Add(new LrcDiagnostic
                {
                    Severity = LrcDiagnosticSeverity.Warning,
                    Code = LrcDiagnosticIds.MaxDiagnosticsReached,
                    Line = line,
                    Column = column,
                    Length = 0,
                    Message = $"Diagnostic cap of {_maxDiagnostics} reached; remaining diagnostics suppressed.",
                });
            }
        }
    }

    public ImmutableArray<LrcDiagnostic> ToImmutableArray() => _items.ToImmutableArray();
}
