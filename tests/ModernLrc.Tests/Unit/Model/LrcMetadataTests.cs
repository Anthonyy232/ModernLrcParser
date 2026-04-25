using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Model;

public sealed class LrcMetadataTests
{
    [Fact]
    public void Empty_HasNoTagsAndNullStrings()
    {
        var m = LrcMetadata.Empty;
        m.RawTags.Count.ShouldBe(0);
        m.Title.ShouldBeNull();
        m.Artist.ShouldBeNull();
        m.Album.ShouldBeNull();
        m.Author.ShouldBeNull();
        m.Lyricist.ShouldBeNull();
        m.CreatedBy.ShouldBeNull();
        m.Tool.ShouldBeNull();
        m.Version.ShouldBeNull();
        m.Length.ShouldBeNull();
        m.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Init_AssignsAllProperties()
    {
        var m = new LrcMetadata
        {
            Title = "Song",
            Artist = "X",
            Album = "Y",
            Author = "Z",
            Lyricist = "L",
            CreatedBy = "B",
            Tool = "T",
            Version = "V",
            Length = TimeSpan.FromSeconds(180),
            Offset = TimeSpan.FromMilliseconds(-150),
            RawTags = [new("ti", "Song"), new("custom", "value")],
        };
        m.Title.ShouldBe("Song");
        m.Length!.Value.TotalSeconds.ShouldBe(180);
        m.Offset.TotalMilliseconds.ShouldBe(-150);
        m.RawTags.Count.ShouldBe(2);
    }

    [Fact]
    public void ContentEquality_HoldsAcrossInstances()
    {
        var a = new LrcMetadata { Title = "A", Artist = "B", RawTags = [new("ti", "A")] };
        var b = new LrcMetadata { Title = "A", Artist = "B", RawTags = [new("ti", "A")] };
        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void ContentEquality_DifferentRawTagsOrder_IsUnequal()
    {
        var a = new LrcMetadata { RawTags = [new("a", "1"), new("b", "2")] };
        var b = new LrcMetadata { RawTags = [new("b", "2"), new("a", "1")] };
        a.ShouldNotBe(b);
    }

    [Fact]
    public void LrcTag_RecordStruct_HasContentEquality()
    {
        var a = new LrcTag("ti", "Song");
        var b = new LrcTag("ti", "Song");
        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void LrcTag_DeconstructsToKeyAndValue()
    {
        var tag = new LrcTag("ar", "X");
        var (key, value) = tag;
        key.ShouldBe("ar");
        value.ShouldBe("X");
    }
}
