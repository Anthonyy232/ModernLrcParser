using System.Collections.Immutable;
using ModernLrc;
using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit;

public sealed class LrcParseResultTests
{
    [Fact]
    public void HasErrors_True_WhenAnyErrorSeverity()
    {
        var result = new LrcParseResult
        {
            Document = LrcDocument.Empty,
            Diagnostics =
            [
                new LrcDiagnostic
                {
                    Severity = LrcDiagnosticSeverity.Error,
                    Code = LrcDiagnosticIds.InvalidTimestamp,
                    Line = 1, Column = 1, Length = 1,
                    Message = "x",
                },
            ],
        };
        result.HasErrors.ShouldBeTrue();
    }

    [Fact]
    public void HasErrors_False_WhenOnlyWarningsAndInfo()
    {
        var result = new LrcParseResult
        {
            Document = LrcDocument.Empty,
            Diagnostics =
            [
                new LrcDiagnostic
                {
                    Severity = LrcDiagnosticSeverity.Warning,
                    Code = "LRC0005",
                    Line = 1, Column = 1, Length = 1,
                    Message = "w",
                },
                new LrcDiagnostic
                {
                    Severity = LrcDiagnosticSeverity.Info,
                    Code = "LRC0030",
                    Line = 2, Column = 1, Length = 1,
                    Message = "i",
                },
            ],
        };
        result.HasErrors.ShouldBeFalse();
    }

    [Fact]
    public void HasErrors_False_WhenEmpty()
    {
        var result = new LrcParseResult
        {
            Document = LrcDocument.Empty,
            Diagnostics = ImmutableArray<LrcDiagnostic>.Empty,
        };
        result.HasErrors.ShouldBeFalse();
    }
}
