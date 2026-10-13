using ModernLrc;
using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class DiagnosticCoverageTests
{
    [Theory]
    [InlineData("[ti:Untitled")]
    [InlineData("[00:01.00 hello")]
    public void LRC0001_UnclosedTag_EmittedAsError(string input)
    {
        var result = LrcParser.Parse(input);
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.UnclosedTag && d.Severity == LrcDiagnosticSeverity.Error);
    }

    [Fact]
    public void LRC0004_UnknownIdTag_EmittedAsInfo()
    {
        var result = LrcParser.Parse("[unknown:value]\n[00:01.00]hello");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.UnknownIdTag && d.Severity == LrcDiagnosticSeverity.Info);
    }

    [Fact]
    public void LRC0005_InvalidOffset_EmittedAsWarning()
    {
        var result = LrcParser.Parse("[offset:not-a-number]\n[00:01.00]hello");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.InvalidOffset && d.Severity == LrcDiagnosticSeverity.Warning);
    }

    [Fact]
    public void LRC0006_InvalidLength_EmittedAsWarning()
    {
        var result = LrcParser.Parse("[length:invalid]\n[00:01.00]hello");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.InvalidLength && d.Severity == LrcDiagnosticSeverity.Warning);
    }

    [Fact]
    public void LRC0007_And_LRC0080_InvalidEnhancedTimestamp_WithNoValidWords()
    {
        var result = LrcParser.Parse("[00:01.00]<bad>text");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.InvalidEnhancedTimestamp && d.Severity == LrcDiagnosticSeverity.Error);
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.EmptyEnhancedWord && d.Severity == LrcDiagnosticSeverity.Info);
    }

    [Fact]
    public void LRC0008_UnclosedEnhancedTimestamp_EmittedAsError()
    {
        var result = LrcParser.Parse("[00:01.00]<00:01.00 incomplete");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.UnclosedEnhancedTimestamp && d.Severity == LrcDiagnosticSeverity.Error);
    }

    [Fact]
    public void LRC0009_EmptyTimestamp_EmittedAsWarning()
    {
        var result = LrcParser.Parse("[][00:01.00]hello");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.EmptyTimestamp && d.Severity == LrcDiagnosticSeverity.Warning);
    }

    [Theory]
    [InlineData("[00:01.0001]hi")]
    [InlineData("[01:23,4567]hi")]
    [InlineData("[00:23:4567]hi")]
    [InlineData("[1:02:33,4567]hi")]
    [InlineData("[1:02:33:4567]hi")]
    [InlineData("[00:01.00]<00:01,4567>word")]
    public void LRC0031_TruncatedPrecision_EmittedForEveryFractionSeparator(string source)
    {
        var result = LrcParser.Parse(source);
        result.HasErrors.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.TruncatedPrecision);
    }

    [Fact]
    public void LRC0031_NotEmittedForThreeDigitFraction()
    {
        var result = LrcParser.Parse("[01:23,450]hi");
        result.Diagnostics.ShouldNotContain(d => d.Code == LrcDiagnosticIds.TruncatedPrecision);
    }

    [Fact]
    public void LRC0050_DroppedUntimedText_EmittedAsInfo()
    {
        var result = LrcParser.Parse("free text without any timestamps\n[00:01.00]ok");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.DroppedUntimedText && d.Severity == LrcDiagnosticSeverity.Info);
    }

    [Fact]
    public void LRC0051_TimestampWithoutText_EmittedAsWarning()
    {
        var result = LrcParser.Parse("[00:01.00]\n[00:02.00]world");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.TimestampWithoutText && d.Severity == LrcDiagnosticSeverity.Warning);
    }

    [Fact]
    public void LRC0070_ImplausibleTimestamp_EmittedAsInfo()
    {
        // 1500 minutes = 25 hours, past the default 24 h threshold.
        var result = LrcParser.Parse("[1500:00.00]way too late");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.ImplausibleTimestamp && d.Severity == LrcDiagnosticSeverity.Info);
    }

    [Fact]
    public void LRC0099_MaxDiagnosticsReached_EmittedOnceWhenCapHit()
    {
        var input = string.Join("\n", Enumerable.Repeat("[bad-ts]junk", 10));
        var options = new LrcParseOptions { MaxDiagnostics = 2 };
        var result = LrcParser.Parse(input, options);
        result.Diagnostics.Count(d => d.Code == LrcDiagnosticIds.MaxDiagnosticsReached).ShouldBe(1);
    }

    [Fact]
    public void MaxDiagnostics_Zero_SuppressesEverythingIncludingCapNotice()
    {
        var options = new LrcParseOptions { MaxDiagnostics = 0 };
        var result = LrcParser.Parse("[bad]a\n[also-bad]b", options);
        result.Diagnostics.ShouldBeEmpty();
    }
}
