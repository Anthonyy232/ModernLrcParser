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
    public void ScatteredMultiTimestampGroup_RoundTripsAsSeparateLines_KeepingVoice()
    {
        // Sorting interleaves the 01s/05s group with the 02s/03s lines, so the writer
        // (which only collapses adjacent lines) emits 05s on its own line.
        const string input =
            "[00:01.00][00:05.00]F: she sings\n" +
            "[00:02.00]still hers\n" +
            "[00:03.00]M: now him\n";

        var reparsed = LrcParser.Parse(LrcWriter.Write(LrcParser.Parse(input).Document)).Document;

        reparsed.Lines.Select(l => (l.Timestamp.TotalMilliseconds, l.GetText(), l.EffectiveVoice)).ShouldBe(
        [
            (1_000L, "she sings", LrcVoice.Female),
            (2_000L, "still hers", LrcVoice.Female),
            (3_000L, "now him", LrcVoice.Male),
            (5_000L, "she sings", LrcVoice.Female),
        ]);
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

    [Fact]
    public void Metadata_WithExpressionEdit_WinsOverStaleRawTags()
    {
        var parsed = LrcParser.Parse("[ti:First]\n[ti:Old]\n[offset:+100]\n[xx:keep]\n[00:01.00]x").Document;
        var edited = parsed with
        {
            Metadata = parsed.Metadata with { Title = "New", Offset = TimeSpan.FromMilliseconds(-50) },
        };

        string written = LrcWriter.Write(edited);

        written.ShouldBe("[ti:New]\n[offset:-50]\n[xx:keep]\n\n[00:01.00]x\n");
    }

    [Fact]
    public void Metadata_WithExpressionClear_DropsRawTags()
    {
        var parsed = LrcParser.Parse("[ti:Old]\n[00:01.00]x").Document;
        var edited = parsed with { Metadata = parsed.Metadata with { Title = null } };

        LrcWriter.Write(edited).ShouldBe("[00:01.00]x\n");
    }

    [Fact]
    public void Metadata_UnparseableKnownTag_PreservedVerbatim()
    {
        var parsed = LrcParser.Parse("[length:abc]\n[00:01.00]x").Document;

        LrcWriter.Write(parsed).ShouldBe("[length:abc]\n\n[00:01.00]x\n");
    }

    [Fact]
    public void Metadata_BuilderRawKnownTag_SetsTypedProperty()
    {
        var doc = new LrcDocumentBuilder()
            .WithRawTag("ti", "Raw")
            .WithRawTag("length", "03:20")
            .AddLine("00:01.00", "x")
            .Build();

        doc.Metadata.Title.ShouldBe("Raw");
        doc.Metadata.Length.ShouldBe(TimeSpan.FromSeconds(200));
        LrcWriter.Write(doc).ShouldBe("[ti:Raw]\n[length:03:20]\n\n[00:01.00]x\n");
    }
}
