using ModernLrc;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class RecoveryTests
{
    [Fact]
    public void BracketContentTolerance_LRC0091_AfterTimestamp_PreservesBrackets()
    {
        // Spec §5.4: after at least one timestamp on a line, a '[…]' group that fails to parse
        // as a timestamp emits LRC0091 and the entire group becomes part of the line text.
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
    public void TruncatedPrecision_LRC0031_EmittedForSubMillisecondInput()
    {
        // 4-digit fraction triggers truncation (only 3 digits = ms precision are kept).
        var result = LrcParser.Parse("[00:01.0001]hello");
        result.HasErrors.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.TruncatedPrecision);
    }

    [Fact]
    public void CrOnlyLineEndings_AreSplitCorrectly()
    {
        // Old-Mac CR-only line separators.
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
        // Stray angle brackets in plain text — should not lose the rest.
        var result = LrcParser.Parse("[00:01.00]a < b > c");
        // Diagnostics still fire (LRC0007 or LRC0008 for the malformed <...>),
        // but the line content is preserved as plain.
        result.Document.Lines.Count.ShouldBe(1);
        var line = result.Document.Lines[0];
        line.ShouldBeOfType<LrcPlainLine>();
        ((LrcPlainLine)line).Text.ShouldBe("a < b > c");
    }

    [Theory]
    [InlineData("eng||")]
    [InlineData("jpn||")]
    [InlineData("chi||")]
    [InlineData("ENG||")]  // case-insensitive
    public void Id3LanguagePrefix_IsStripped_WithLRC0092(string prefix)
    {
        var result = LrcParser.Parse($"{prefix}[ti:Demo]\n[00:01.00]hello");
        result.HasErrors.ShouldBeFalse();
        result.Document.Metadata.Title.ShouldBe("Demo");
        result.Document.Lines.Count.ShouldBe(1);
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.Id3LanguagePrefixStripped);
    }

    [Fact]
    public void Id3LanguagePrefix_NotStripped_WhenNotAtStart()
    {
        // The prefix must be at the very start of input.
        var result = LrcParser.Parse("[ti:Demo]\neng||more");
        result.Diagnostics.ShouldNotContain(d => d.Code == LrcDiagnosticIds.Id3LanguagePrefixStripped);
    }
}
