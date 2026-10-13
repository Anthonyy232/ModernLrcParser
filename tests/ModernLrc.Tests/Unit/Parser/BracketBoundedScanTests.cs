using ModernLrc;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

/// <summary>The LRC format is line-based: every <c>[…]</c> tag closes on the same line as the
/// matching <c>[</c>. The scanner enforces this so a stray <c>]</c> on a later line cannot be
/// absorbed into a multi-line tag value (which would also corrupt diagnostic line tracking on
/// every emission after it).</summary>
public sealed class BracketBoundedScanTests
{
    [Fact]
    public void IdTag_OpenedOnOneLine_ClosedOnAnother_EmitsUnclosedTag()
    {
        var result = LrcParser.Parse("[ti:Some\nTitle]\n[00:01.00]hello");

        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.UnclosedTag
            && d.Severity == LrcDiagnosticSeverity.Error
            && d.Line == 1);

        result.Document.Metadata.Title.ShouldBeNull();
    }

    [Fact]
    public void Timestamp_OpenedOnOneLine_ClosedOnAnother_EmitsUnclosedTag()
    {
        var result = LrcParser.Parse("[00:01.00\n]hello");

        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.UnclosedTag
            && d.Severity == LrcDiagnosticSeverity.Error
            && d.Line == 1);

        // The orphan ']hello' on line 2 has no leading '[' so it becomes free text → LRC0050.
        result.Document.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void DiagnosticLineNumbers_StayAccurate_AfterUnclosedTagRecovery()
    {
        var result = LrcParser.Parse(
            "[ti:Some\nTitle]\n" +    // line 1: unclosed [, then on line 2 free text 'Title]'
            "[00:01.00]ok\n" +        // line 3: valid
            "[bad]junk");             // line 4: malformed ID tag (LRC0003)

        var lrc0003 = result.Diagnostics.SingleOrDefault(d => d.Code == LrcDiagnosticIds.MalformedIdTag);
        lrc0003.ShouldNotBeNull();
        lrc0003!.Line.ShouldBe(4, "Line counter must remain accurate after recovering from an unclosed tag.");
    }

    [Fact]
    public void RoundTrip_TagWithEmbeddedNewlineInBuilder_DoesNotSurviveReparse()
    {
        // Known limitation: the writer emits the newline verbatim, and the parser rejects it loudly.
        var doc = new LrcDocumentBuilder().WithTitle("Multi\nLine").AddLine("00:01.00", "x").Build();
        var lrc = LrcWriter.Write(doc);

        var reparsed = LrcParser.Parse(lrc);
        reparsed.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.UnclosedTag);
        reparsed.Document.Metadata.Title.ShouldBeNull();
    }
}
