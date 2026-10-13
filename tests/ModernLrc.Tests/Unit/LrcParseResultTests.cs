using ModernLrc;
using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit;

public sealed class LrcParseResultTests
{
    [Fact]
    public void HasErrorsAndHasWarnings_LookOnlyAtTheirSeverity()
    {
        var result = new LrcParseResult
        {
            Document = LrcDocument.Empty,
            Diagnostics =
            [
                new LrcDiagnostic
                {
                    Severity = LrcDiagnosticSeverity.Warning,
                    Code = LrcDiagnosticIds.InvalidOffset,
                    Line = 1, Column = 1, Length = 1,
                    Message = "w",
                },
                new LrcDiagnostic
                {
                    Severity = LrcDiagnosticSeverity.Info,
                    Code = LrcDiagnosticIds.NonStandardTimestamp,
                    Line = 2, Column = 1, Length = 1,
                    Message = "i",
                },
            ],
        };
        result.HasErrors.ShouldBeFalse();
        result.HasWarnings.ShouldBeTrue();
    }
}
