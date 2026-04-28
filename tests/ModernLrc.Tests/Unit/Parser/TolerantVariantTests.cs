using ModernLrc;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class TolerantVariantTests
{
    [Theory]
    [InlineData("[00:01]hello")]          // no fraction
    [InlineData("[00:01.500]hello")]      // ms precision
    [InlineData("[00:01:50]hello")]       // colon as fraction separator
    public void NonStandardVariant_IsAcceptedWithLRC0030(string source)
    {
        var result = LrcParser.Parse(source);
        result.HasErrors.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Code == "LRC0030");
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void Canonical_DoesNotEmitLRC0030()
    {
        var result = LrcParser.Parse("[00:01.00]hello");
        result.Diagnostics.ShouldNotContain(d => d.Code == "LRC0030");
    }

    [Theory]
    [InlineData("[1:02:33.45]hello", 1L * 60L * 60L * 1000L + 2L * 60L * 1000L + 33L * 1000L + 450L)]
    [InlineData("[2:00:00.00]two-hours", 2L * 60L * 60L * 1000L)]
    public void HoursNotation_IsAcceptedWithLRC0030(string input, long expectedMs)
    {
        var result = LrcParser.Parse(input);
        result.HasErrors.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.NonStandardTimestamp);
        result.Document.Lines.Count.ShouldBe(1);
        ((long)((LrcPlainLine)result.Document.Lines[0]).Timestamp.TotalMilliseconds).ShouldBe(expectedMs);
    }

    [Theory]
    [InlineData("[01:23,45]hello", 83_450L)]    // mm:ss,ff
    [InlineData("[01:23,450]hello", 83_450L)]   // mm:ss,fff
    public void CommaDecimal_IsAcceptedWithLRC0030(string input, long expectedMs)
    {
        var result = LrcParser.Parse(input);
        result.HasErrors.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.NonStandardTimestamp);
        result.Document.Lines.Count.ShouldBe(1);
        ((long)((LrcPlainLine)result.Document.Lines[0]).Timestamp.TotalMilliseconds).ShouldBe(expectedMs);
    }
}
