using System.Collections.Immutable;
using ModernLrc.Diagnostics;

namespace ModernLrc.Internal;

/// <summary>Collects diagnostics during parsing. Respects the <see cref="LrcParseOptions.MaxDiagnostics"/>
/// cap (emits <c>LRC0099</c> once when the cap is hit). In Strict mode, the first Error-severity
/// diagnostic causes the parser to bail and the entry point to throw <see cref="LrcParseException"/>.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by LrcParser.Parse via LrcScanner.")]
internal sealed class LrcDiagnosticEmitter
{
    private readonly List<LrcDiagnostic> _items = new();
    private readonly int _maxDiagnostics;
    private bool _capReached;

    /// <summary>The first Error-severity diagnostic emitted (used when Strict mode bails).</summary>
    public LrcDiagnostic? FirstError { get; private set; }

    /// <summary>True after a Strict-mode parse has hit its first Error and unwinding should begin.</summary>
    public bool StrictBail { get; private set; }

    /// <summary>True once <see cref="LrcStrictness.Strict"/> is configured.</summary>
    public bool IsStrict { get; }

    /// <summary>Initialises the emitter from the given parse options.</summary>
    public LrcDiagnosticEmitter(LrcParseOptions options)
    {
        _maxDiagnostics = options.MaxDiagnostics;
        IsStrict = options.Strictness == LrcStrictness.Strict;
    }

    /// <summary>Emit a diagnostic. Returns true when scanning should continue, false when Strict mode says bail.</summary>
    public bool Emit(LrcDiagnosticSeverity severity, string code, int line, int column, int length, string message)
    {
        if (_capReached) return !IsStrict || severity != LrcDiagnosticSeverity.Error;

        if (_items.Count >= _maxDiagnostics)
        {
            _capReached = true;
            // MaxDiagnostics == 0 means the caller wants total silence — don't emit the cap
            // notice itself. Any non-zero cap emits exactly one LRC0099 then suppresses.
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
            // FirstError must still be tracked so Strict mode bails correctly even when
            // the cap suppresses the diagnostic body.
            if (severity == LrcDiagnosticSeverity.Error && FirstError is null)
            {
                FirstError = new LrcDiagnostic
                {
                    Severity = severity,
                    Code = code,
                    Line = line,
                    Column = column,
                    Length = length,
                    Message = message,
                };
                if (IsStrict) { StrictBail = true; return false; }
            }
            return !IsStrict || severity != LrcDiagnosticSeverity.Error;
        }

        var diag = new LrcDiagnostic
        {
            Severity = severity,
            Code = code,
            Line = line,
            Column = column,
            Length = length,
            Message = message,
        };

        _items.Add(diag);

        if (severity == LrcDiagnosticSeverity.Error)
        {
            FirstError ??= diag;
            if (IsStrict)
            {
                StrictBail = true;
                return false;
            }
        }
        return true;
    }

    /// <summary>Snapshot the collected diagnostics (in source order).</summary>
    public ImmutableArray<LrcDiagnostic> ToImmutableArray() => _items.ToImmutableArray();
}
