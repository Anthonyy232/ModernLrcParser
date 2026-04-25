using ModernLrc;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class TryParseTests
{
    [Fact]
    public void TryParse_ValidString_ReturnsTrue()
    {
        var ok = LrcParser.TryParse("[00:01.00]hi", out var doc);
        ok.ShouldBeTrue();
        doc.ShouldNotBeNull();
        doc!.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void TryParse_ErrorContent_ReturnsFalse()
    {
        var ok = LrcParser.TryParse("[bad]junk", out var doc);
        ok.ShouldBeFalse();
        doc.ShouldBeNull();
    }

    [Fact]
    public void TryParse_WithDiagnostics_ProvidesDetail()
    {
        var ok = LrcParser.TryParse(
            "[bad]junk".AsSpan(), out var doc, out var diagnostics);
        ok.ShouldBeFalse();
        // "[bad]junk" — 'b' is alpha → malformed ID tag (no colon) → LRC0003
        diagnostics.ShouldContain(d => d.Code == "LRC0003");
    }
}
