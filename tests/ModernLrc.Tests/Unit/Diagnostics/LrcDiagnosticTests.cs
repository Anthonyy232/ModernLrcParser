using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit.Diagnostics;

public sealed class LrcDiagnosticTests
{
    [Fact]
    public void Construct_AllPropertiesAccessible()
    {
        var d = new LrcDiagnostic
        {
            Severity = LrcDiagnosticSeverity.Error,
            Code = LrcDiagnosticIds.InvalidTimestamp,
            Line = 5,
            Column = 12,
            Length = 3,
            Message = "bad timestamp",
        };
        d.Severity.ShouldBe(LrcDiagnosticSeverity.Error);
        d.Code.ShouldBe("LRC0002");
        d.Line.ShouldBe(5);
        d.Column.ShouldBe(12);
        d.Length.ShouldBe(3);
        d.Message.ShouldBe("bad timestamp");
    }

    [Fact]
    public void ContentEquality_HoldsAcrossInstances()
    {
        var a = new LrcDiagnostic
        {
            Severity = LrcDiagnosticSeverity.Warning,
            Code = "LRC0005",
            Line = 1, Column = 1, Length = 1,
            Message = "x",
        };
        var b = new LrcDiagnostic
        {
            Severity = LrcDiagnosticSeverity.Warning,
            Code = "LRC0005",
            Line = 1, Column = 1, Length = 1,
            Message = "x",
        };
        a.ShouldBe(b);
    }
}
