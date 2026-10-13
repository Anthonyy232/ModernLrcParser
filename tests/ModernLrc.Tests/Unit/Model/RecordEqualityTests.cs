using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Model;

/// <summary>Records holding <see cref="EquatableArray{T}"/> compare by content, all the way down.</summary>
public sealed class RecordEqualityTests
{
    private static LrcDocument BuildDoc()
    {
        var t = LrcTimestamp.FromMilliseconds(1_000);
        return new LrcDocument
        {
            Metadata = new LrcMetadata { Title = "X", RawTags = [new("ti", "X"), new("custom", "v")] },
            Lines =
            [
                new LrcPlainLine { Timestamp = t, Text = "a" },
                new LrcEnhancedLine { Timestamp = t, Words = [new(t, "b")] },
            ],
        };
    }

    [Fact]
    public void Document_SeparateInstancesWithSameContent_AreEqual()
    {
        var a = BuildDoc();
        var b = BuildDoc();
        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Metadata_RawTagOrder_AffectsEquality()
    {
        var a = new LrcMetadata { RawTags = [new("a", "1"), new("b", "2")] };
        var b = new LrcMetadata { RawTags = [new("b", "2"), new("a", "1")] };
        a.ShouldNotBe(b);
    }
}
