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

    [Fact]
    public void Metadata_DuplicateTypedRawTags_RoundTrip()
    {
        const string input = "[ti:First]\n[ti:Last]\n[00:01.00]x";

        var parsed = LrcParser.Parse(input);
        string written = LrcWriter.Write(parsed.Document);
        var reparsed = LrcParser.Parse(written);

        reparsed.Document.Metadata.Title.ShouldBe("Last");
        reparsed.Document.Metadata.RawTags.Count.ShouldBe(2);
        reparsed.Document.Metadata.RawTags[0].ShouldBe(new LrcTag("ti", "First"));
        reparsed.Document.Metadata.RawTags[1].ShouldBe(new LrcTag("ti", "Last"));
    }

    [Fact]
    public void Metadata_BuilderEditedTypedAccessor_ReplacesRawTags()
    {
        var parsed = LrcParser.Parse("[ti:Original]\n[00:01.00]x").Document;
        var edited = new LrcDocumentBuilder(parsed)
            .WithTitle("Edited")
            .Build();

        var reparsed = LrcParser.Parse(LrcWriter.Write(edited));

        reparsed.Document.Metadata.Title.ShouldBe("Edited");
        reparsed.Document.Metadata.RawTags.Count.ShouldBe(1);
        reparsed.Document.Metadata.RawTags[0].ShouldBe(new LrcTag("ti", "Edited"));
    }
}
