using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Model;

public sealed class LrcDocumentTests
{
    [Fact]
    public void Empty_HasEmptyMetadataAndNoLines()
    {
        var doc = LrcDocument.Empty;
        doc.Metadata.ShouldBe(LrcMetadata.Empty);
        doc.Lines.Count.ShouldBe(0);
    }

    [Fact]
    public void Init_AssignsMetadataAndLines()
    {
        var t = LrcTimestamp.FromMilliseconds(1_000);
        var doc = new LrcDocument
        {
            Metadata = new LrcMetadata { Title = "X" },
            Lines =
            [
                new LrcPlainLine { Timestamps = [t], Text = "hello" },
            ],
        };
        doc.Metadata.Title.ShouldBe("X");
        doc.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void ContentEquality_HoldsAcrossInstances()
    {
        var t = LrcTimestamp.FromMilliseconds(1_000);
        var a = new LrcDocument
        {
            Metadata = new LrcMetadata { Title = "X" },
            Lines = [new LrcPlainLine { Timestamps = [t], Text = "a" }],
        };
        var b = new LrcDocument
        {
            Metadata = new LrcMetadata { Title = "X" },
            Lines = [new LrcPlainLine { Timestamps = [t], Text = "a" }],
        };
        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }
}
