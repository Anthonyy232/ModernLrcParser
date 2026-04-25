using ModernLrc;
using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class DiagnosticCoverageTests
{
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
    public void LRC0007_InvalidEnhancedTimestamp_EmittedAsError()
    {
        // <bad> inside an enhanced line — "bad" is not a valid timestamp.
        var result = LrcParser.Parse("[00:01.00]<bad>text");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.InvalidEnhancedTimestamp && d.Severity == LrcDiagnosticSeverity.Error);
    }

    [Fact]
    public void LRC0008_UnclosedEnhancedTimestamp_EmittedAsError()
    {
        // '<' with no closing '>' before EOL.
        var result = LrcParser.Parse("[00:01.00]<00:01.00 incomplete");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.UnclosedEnhancedTimestamp && d.Severity == LrcDiagnosticSeverity.Error);
    }

    [Fact]
    public void LRC0009_EmptyTimestamp_EmittedAsWarning()
    {
        // '[]' before a valid timestamp on the same line.
        var result = LrcParser.Parse("[][00:01.00]hello");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.EmptyTimestamp && d.Severity == LrcDiagnosticSeverity.Warning);
    }

    [Fact]
    public void LRC0050_DroppedUntimedText_EmittedAsInfo()
    {
        // A line with no timestamp is dropped and LRC0050 is emitted.
        var result = LrcParser.Parse("free text without any timestamps\n[00:01.00]ok");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.DroppedUntimedText && d.Severity == LrcDiagnosticSeverity.Info);
    }

    [Fact]
    public void LRC0051_TimestampWithoutText_EmittedAsWarning()
    {
        // First line has a timestamp but nothing follows it before the newline.
        var result = LrcParser.Parse("[00:01.00]\n[00:02.00]world");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.TimestampWithoutText && d.Severity == LrcDiagnosticSeverity.Warning);
    }

    [Fact]
    public void LRC0070_ImplausibleTimestamp_EmittedAsInfo()
    {
        // 1500 minutes = 25 hours, exceeds the default 24 h threshold.
        // The check only runs when ImplausibleTimestampThreshold > TimeSpan.Zero (the default is 24 h).
        var result = LrcParser.Parse("[1500:00.00]way too late");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.ImplausibleTimestamp && d.Severity == LrcDiagnosticSeverity.Info);
    }

    [Fact]
    public void LRC0080_EmptyEnhancedWord_EmittedAsInfo()
    {
        // <bad> fails to parse as a timestamp (LRC0007 fires, word is skipped).
        // Because no words succeed, LRC0080 fires as well.
        var result = LrcParser.Parse("[00:01.00]<bad>text");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.EmptyEnhancedWord && d.Severity == LrcDiagnosticSeverity.Info);
    }

    [Fact]
    public void LRC0099_MaxDiagnosticsReached_EmittedOnceWhenCapHit()
    {
        // 10 malformed ID tags each emit LRC0003; cap at 2 forces LRC0099 on the third.
        var input = string.Join("\n", Enumerable.Repeat("[bad-ts]junk", 10));
        var options = new LrcParseOptions { MaxDiagnostics = 2 };
        var result = LrcParser.Parse(input, options);
        result.Diagnostics.Count(d => d.Code == LrcDiagnosticIds.MaxDiagnosticsReached).ShouldBe(1);
    }
}
