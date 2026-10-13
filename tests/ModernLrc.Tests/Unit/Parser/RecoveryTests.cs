using ModernLrc;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class RecoveryTests
{
    [Fact]
    public void BracketContentTolerance_LRC0091_AfterTimestamp_PreservesBrackets()
    {
        var result = LrcParser.Parse("[00:01.00][scared]more");
        result.HasErrors.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.BracketedContentTolerance);
        result.Document.Lines.Count.ShouldBe(1);
        var line = (LrcPlainLine)result.Document.Lines[0];
        line.Text.ShouldBe("[scared]more");
    }

    [Fact]
    public void LinesReordered_LRC0060_EmittedWhenSortReorders()
    {
        var result = LrcParser.Parse("[00:02.00]b\n[00:01.00]a");
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.LinesReordered);
    }

    [Fact]
    public void LinesReordered_LRC0060_NotEmittedWhenAlreadySorted()
    {
        var result = LrcParser.Parse("[00:01.00]a\n[00:02.00]b");
        result.Diagnostics.ShouldNotContain(d => d.Code == LrcDiagnosticIds.LinesReordered);
    }

    [Fact]
    public void ConflictingMetadata_LRC0020_EmittedOnDifferingValues()
    {
        var result = LrcParser.Parse("[ti:First]\n[ti:Second]\n[00:01.00]x");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.ConflictingMetadata && d.Severity == LrcDiagnosticSeverity.Warning);
        result.Document.Metadata.Title.ShouldBe("Second");
    }

    [Fact]
    public void DuplicateMetadata_LRC0021_EmittedOnIdenticalValues()
    {
        var result = LrcParser.Parse("[ti:Same]\n[ti:Same]\n[00:01.00]x");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.DuplicateMetadata && d.Severity == LrcDiagnosticSeverity.Info);
    }

    [Fact]
    public void CrOnlyLineEndings_AreSplitCorrectly()
    {
        var src = "[00:01.00]a\r[00:02.00]b\r[00:03.00]c";
        var result = LrcParser.Parse(src);
        result.HasErrors.ShouldBeFalse();
        result.Document.Lines.Count.ShouldBe(3);
        ((LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("a");
        ((LrcPlainLine)result.Document.Lines[1]).Text.ShouldBe("b");
        ((LrcPlainLine)result.Document.Lines[2]).Text.ShouldBe("c");
    }

    [Fact]
    public void DigitPrefixedMalformedTimestamp_EmitsLRC0002_NotLRC0004()
    {
        // Out-of-range seconds — must not silently degrade to "unknown ID tag".
        var result = LrcParser.Parse("[00:99.00]should-not-be-lost\n[00:01.00]ok");
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.InvalidTimestamp);
        result.Diagnostics.ShouldNotContain(d => d.Code == LrcDiagnosticIds.UnknownIdTag);
    }

    [Fact]
    public void EnhancedParseFailure_DemotesToPlainLine_PreservesText()
    {
        var result = LrcParser.Parse("[00:01.00]a < b > c");
        result.Document.Lines.Count.ShouldBe(1);
        var line = result.Document.Lines[0];
        line.ShouldBeOfType<LrcPlainLine>();
        ((LrcPlainLine)line).Text.ShouldBe("a < b > c");
    }

    [Fact]
    public void EnhancedParseFailure_AfterValidWord_PreservesTrailingText()
    {
        var result = LrcParser.Parse("[00:01.00]<00:01.00>ok <bad>lost");

        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.InvalidEnhancedTimestamp);
        result.Diagnostics.ShouldNotContain(d => d.Code == LrcDiagnosticIds.DroppedUntimedText);
        result.Document.Lines.Count.ShouldBe(1);

        var line = result.Document.Lines[0].ShouldBeOfType<LrcEnhancedLine>();
        line.Words.Count.ShouldBe(1);
        line.Words[0].Text.ShouldBe("ok <bad>lost");
        line.GetText().ShouldBe("ok <bad>lost");
    }

    [Theory]
    [InlineData("en||")]
    [InlineData("eng||")]
    [InlineData("ENG||")]
    public void Id3LanguagePrefix_IsStripped_WithLRC0092(string prefix)
    {
        var result = LrcParser.Parse($"{prefix}[ti:Demo]\n[00:01.00]hello");
        result.HasErrors.ShouldBeFalse();
        result.Document.Metadata.Title.ShouldBe("Demo");
        ((LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("hello");
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.Id3LanguagePrefixStripped);
    }

    [Theory]
    [InlineData("e||[ti:Demo]")]
    [InlineData("[ti:Demo]\neng||more")]
    public void Id3LanguagePrefix_NotStripped_WhenNotTwoOrThreeLettersAtStart(string input)
    {
        var result = LrcParser.Parse(input);
        result.Diagnostics.ShouldNotContain(d => d.Code == LrcDiagnosticIds.Id3LanguagePrefixStripped);
    }
}
