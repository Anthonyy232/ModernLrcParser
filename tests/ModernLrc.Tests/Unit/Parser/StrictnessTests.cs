using ModernLrc;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class StrictnessTests
{
    // "[bad]junk" — 'b' is alpha so it's treated as a malformed ID tag (LRC0003).
    [Fact]
    public void Strict_ThrowsOnFirstError()
    {
        var options = new LrcParseOptions { Strictness = LrcStrictness.Strict };
        var ex = Should.Throw<LrcParseException>(() =>
            LrcParser.Parse("[bad]junk", options));
        ex.FirstError.ShouldNotBeNull();
        ex.FirstError!.Code.ShouldBe("LRC0003");
        ex.PartialResult.ShouldNotBeNull();
    }

    [Fact]
    public void Tolerant_CollectsAllDiagnostics()
    {
        var result = LrcParser.Parse("[bad]junk\n[also-bad]more");
        result.HasErrors.ShouldBeTrue();
        result.Diagnostics.Count(d => d.Code == "LRC0003").ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Tolerant_RecoversAfterMalformedLine()
    {
        var result = LrcParser.Parse("[bad]junk\n[00:01.00]good");
        result.Document.Lines.Count.ShouldBe(1);
        ((ModernLrc.Model.LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("good");
    }

    [Fact]
    public void Strict_MaxDiagnosticsZero_StillThrowsWhenSuppressedErrorArrivesAfterWarning()
    {
        var options = new LrcParseOptions
        {
            Strictness = LrcStrictness.Strict,
            MaxDiagnostics = 0,
        };

        var ex = Should.Throw<LrcParseException>(() =>
            LrcParser.Parse("[]\n[bad]junk", options));

        ex.FirstError.ShouldNotBeNull();
        ex.FirstError!.Code.ShouldBe("LRC0003");
        ex.PartialResult!.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void Strict_ErrorAfterDiagnosticCap_StillThrows()
    {
        var options = new LrcParseOptions
        {
            Strictness = LrcStrictness.Strict,
            MaxDiagnostics = 1,
        };

        var ex = Should.Throw<LrcParseException>(() =>
            LrcParser.Parse("[]\n[]\n[bad]junk", options));

        ex.FirstError.ShouldNotBeNull();
        ex.FirstError!.Code.ShouldBe("LRC0003");
    }
}
