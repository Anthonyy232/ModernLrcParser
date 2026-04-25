using ModernLrc;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class EstimateSizeTests
{
    [Fact]
    public void Empty_ReturnsZero()
    {
        LrcWriter.EstimateSize(LrcDocument.Empty).ShouldBe(0);
    }

    [Fact]
    public void NonEmpty_GreaterThanOrEqualActualOutput()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("X")
            .AddLine("00:01.00", "hello")
            .Build();
        var actual = LrcWriter.Write(doc).Length;
        var estimate = LrcWriter.EstimateSize(doc);
        estimate.ShouldBeGreaterThanOrEqualTo(actual);
    }

    [Fact]
    public void EstimateByteSize_AlwaysGreaterThanEstimateSize()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();
        LrcWriter.EstimateByteSize(doc).ShouldBeGreaterThan(LrcWriter.EstimateSize(doc));
    }
}
