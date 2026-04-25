using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class RoundTripTests
{
    [Fact]
    public void Plain_AuthorWriteParseEqualsOriginal()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("Round Trip")
            .WithArtist("Tester")
            .AddLine("00:01.00", "first")
            .AddLine("00:02.50", "second")
            .Build();
        var lrc = LrcWriter.Write(doc);
        var parsed = LrcParser.Parse(lrc);
        parsed.HasErrors.ShouldBeFalse();
        parsed.Document.Metadata.Title.ShouldBe("Round Trip");
        parsed.Document.Lines.Count.ShouldBe(2);
    }

    [Fact]
    public void Enhanced_AuthorWriteParse_PreservesWordTimings()
    {
        var t1 = LrcTimestamp.FromMilliseconds(1_000);
        var t2 = LrcTimestamp.FromMilliseconds(1_500);
        var doc = new LrcDocumentBuilder()
            .AddEnhancedLine(t1, [(t1, "hello "), (t2, "world")])
            .Build();
        var lrc = LrcWriter.Write(doc);
        var parsed = LrcParser.Parse(lrc);
        parsed.HasErrors.ShouldBeFalse();
        var line = (LrcEnhancedLine)parsed.Document.Lines[0];
        line.Words.Count.ShouldBe(2);
        line.Words[0].Text.ShouldBe("hello ");
        line.Words[1].Text.ShouldBe("world");
    }

    [Fact]
    public void Voice_AuthorWriteParse_PreservesVoiceState()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "her line", LrcVoice.Female)
            .AddLine("00:02.00", "still her", LrcVoice.Female)
            .Build();
        var lrc = LrcWriter.Write(doc);
        var parsed = LrcParser.Parse(lrc);
        parsed.HasErrors.ShouldBeFalse();
        ((LrcPlainLine)parsed.Document.Lines[0]).EffectiveVoice.ShouldBe(LrcVoice.Female);
        ((LrcPlainLine)parsed.Document.Lines[1]).EffectiveVoice.ShouldBe(LrcVoice.Female);
    }
}
