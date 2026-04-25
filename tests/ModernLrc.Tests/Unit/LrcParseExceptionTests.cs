using System.Collections.Immutable;
using ModernLrc;
using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit;

public sealed class LrcParseExceptionTests
{
    [Fact]
    public void MessageOnly_AllRichPropertiesNull()
    {
        var ex = new LrcParseException("boom");
        ex.Message.ShouldBe("boom");
        ex.PartialResult.ShouldBeNull();
        ex.FirstError.ShouldBeNull();
        ex.FilePath.ShouldBeNull();
    }

    [Fact]
    public void RichConstructor_PreservesAllProperties()
    {
        var diag = new LrcDiagnostic
        {
            Severity = LrcDiagnosticSeverity.Error,
            Code = LrcDiagnosticIds.InvalidTimestamp,
            Line = 1, Column = 2, Length = 3,
            Message = "bad",
        };
        var partial = new LrcParseResult
        {
            Document = LrcDocument.Empty,
            Diagnostics = ImmutableArray.Create(diag),
        };
        var ex = new LrcParseException("strict-mode failure", partial, diag, "C:/foo.lrc");
        ex.Message.ShouldBe("strict-mode failure");
        ex.PartialResult.ShouldBe(partial);
        ex.FirstError.ShouldBe(diag);
        ex.FilePath.ShouldBe("C:/foo.lrc");
    }
}
