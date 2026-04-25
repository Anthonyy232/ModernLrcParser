using System.Text;
using ModernLrc;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

/// <summary>Tests added to close gaps identified by a test-coverage audit:
/// LRC0001 (unclosed tag), Strict-mode throws for non-LRC0003 errors, UTF-16 BE input,
/// async non-MemoryStream cancellation, multi-timestamp + voice roundtrip.</summary>
public sealed class AdditionalCoverageTests
{
    // -------------------------------------------------------------------------
    // LRC0001 UnclosedTag — exercised at the line scanner's '[' path.
    // -------------------------------------------------------------------------

    [Fact]
    public void LRC0001_UnclosedTag_AtMetadataStart_EmittedAsError()
    {
        // '[ti:Untitled' — no closing ']' before EOL.
        var result = LrcParser.Parse("[ti:Untitled");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.UnclosedTag && d.Severity == LrcDiagnosticSeverity.Error);
    }

    [Fact]
    public void LRC0001_UnclosedTag_AtTimestampPosition_EmittedAsError()
    {
        // '[00:01.00' — no closing ']' before EOL.
        var result = LrcParser.Parse("[00:01.00 hello");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.UnclosedTag && d.Severity == LrcDiagnosticSeverity.Error);
    }

    // -------------------------------------------------------------------------
    // Strict mode — verifies bail on Error-severity codes other than LRC0003.
    // -------------------------------------------------------------------------

    [Fact]
    public void Strict_LRC0002_InvalidTimestamp_Throws()
    {
        // Digit-prefixed bracket that fails timestamp parsing → LRC0002 Error.
        var options = new LrcParseOptions { Strictness = LrcStrictness.Strict };
        var ex = Should.Throw<LrcParseException>(() =>
            LrcParser.Parse("[00:99.00]hello", options));
        ex.FirstError.ShouldNotBeNull();
        ex.FirstError!.Code.ShouldBe(LrcDiagnosticIds.InvalidTimestamp);
    }

    [Fact]
    public void Strict_LRC0007_InvalidEnhancedTimestamp_Throws()
    {
        var options = new LrcParseOptions { Strictness = LrcStrictness.Strict };
        var ex = Should.Throw<LrcParseException>(() =>
            LrcParser.Parse("[00:01.00]<bad>text", options));
        ex.FirstError!.Code.ShouldBe(LrcDiagnosticIds.InvalidEnhancedTimestamp);
    }

    [Fact]
    public void Strict_LRC0008_UnclosedEnhancedTimestamp_Throws()
    {
        var options = new LrcParseOptions { Strictness = LrcStrictness.Strict };
        var ex = Should.Throw<LrcParseException>(() =>
            LrcParser.Parse("[00:01.00]<00:01.00 oops", options));
        ex.FirstError!.Code.ShouldBe(LrcDiagnosticIds.UnclosedEnhancedTimestamp);
    }

    // -------------------------------------------------------------------------
    // Encoding pipeline — UTF-16 BE input (only LE was previously tested).
    // -------------------------------------------------------------------------

    [Fact]
    public void Utf16Be_DecodedFromBom()
    {
        var bytes = new byte[] { 0xFE, 0xFF }
            .Concat(Encoding.BigEndianUnicode.GetBytes("[00:01.00]big-endian"))
            .ToArray();
        var result = LrcParser.Parse(bytes);
        result.Document.Lines.Count.ShouldBe(1);
        ((LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("big-endian");
    }

    // -------------------------------------------------------------------------
    // Async — file-backed stream (a stream type that does not expose a buffer for fast access).
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ParseAsync_FileBackedStream_ProducesExpectedDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        var tmp = Path.Combine(Path.GetTempPath(), $"modernlrc-async-{Guid.NewGuid():N}.lrc");
        try
        {
            await File.WriteAllTextAsync(tmp, "[00:01.00]from-file-stream", ct).ConfigureAwait(true);
            await using var fs = File.OpenRead(tmp);
            var result = await LrcParser.ParseAsync(fs, cancellationToken: ct).ConfigureAwait(true);
            result.Document.Lines.Count.ShouldBe(1);
            ((LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("from-file-stream");
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task ParseAsync_FileBackedStream_PreCancelledToken_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        var tmp = Path.Combine(Path.GetTempPath(), $"modernlrc-cancel-{Guid.NewGuid():N}.lrc");
        try
        {
            await File.WriteAllTextAsync(tmp, "[00:01.00]x", ct).ConfigureAwait(true);
            await using var fs = File.OpenRead(tmp);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync().ConfigureAwait(true);
            await Should.ThrowAsync<OperationCanceledException>(
                () => LrcParser.ParseAsync(fs, cancellationToken: cts.Token).AsTask()).ConfigureAwait(true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    // -------------------------------------------------------------------------
    // MaxDiagnostics — boundary behaviour around 0 and Strict mode.
    // -------------------------------------------------------------------------

    [Fact]
    public void MaxDiagnostics_Zero_SuppressesEverythingIncludingCapNotice()
    {
        // Three malformed lines would each fire LRC0003 (Error). With MaxDiagnostics = 0
        // the result must contain ZERO diagnostics — including the LRC0099 cap notice.
        var options = new LrcParseOptions { MaxDiagnostics = 0 };
        var result = LrcParser.Parse("[bad]a\n[also-bad]b\n[third-bad]c", options);
        result.Diagnostics.Length.ShouldBe(0);
        result.HasErrors.ShouldBeFalse();
        result.HasWarnings.ShouldBeFalse();
    }

    [Fact]
    public void MaxDiagnostics_Zero_StrictModeStillThrowsOnFirstError()
    {
        // Even with the cap silencing the diagnostic body, Strict mode must still bail
        // and the thrown exception must carry FirstError.
        var options = new LrcParseOptions { MaxDiagnostics = 0, Strictness = LrcStrictness.Strict };
        var ex = Should.Throw<LrcParseException>(() =>
            LrcParser.Parse("[bad]a", options));
        ex.FirstError.ShouldNotBeNull();
        ex.FirstError!.Code.ShouldBe(LrcDiagnosticIds.MalformedIdTag);
    }

    // -------------------------------------------------------------------------
    // LRC0031 (TruncatedPrecision) must fire for ALL fraction separators —
    // not just '.'. Comma decimal and colon-fraction with 4+ digits truncate the
    // same way, and the diagnostic was previously missed.
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("[01:23,4567]hi")]              // mm:ss,ffff (comma decimal, 4 digits)
    [InlineData("[00:23:4567]hi")]              // mm:ss:ffff (colon-fraction, first segment zero)
    [InlineData("[1:02:33,4567]hi")]            // h:mm:ss,ffff (hours + comma)
    [InlineData("[1:02:33:4567]hi")]            // h:mm:ss:ffff (hours + colon-fraction)
    public void LRC0031_TruncatedPrecision_NonDotSeparators(string source)
    {
        var result = LrcParser.Parse(source);
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.TruncatedPrecision,
            customMessage: $"Expected LRC0031 for '{source}'.");
    }

    [Fact]
    public void LRC0031_NotEmittedForThreeDigitCommaFraction()
    {
        // Three-digit fraction is exact ms — no truncation, no LRC0031.
        var result = LrcParser.Parse("[01:23,450]hi");
        result.Diagnostics.ShouldNotContain(d => d.Code == LrcDiagnosticIds.TruncatedPrecision);
    }

    [Fact]
    public void LRC0031_EmittedForEnhancedWord_CommaSeparator()
    {
        var result = LrcParser.Parse("[00:01.00]<00:01,4567>word");
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.TruncatedPrecision);
    }

    // -------------------------------------------------------------------------
    // ID3 language prefix: 2-letter (ISO 639-1) codes must strip too —
    // previously only 3-letter (ISO 639-2) was matched.
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("en||")]    // ISO 639-1 English
    [InlineData("ja||")]    // ISO 639-1 Japanese
    [InlineData("zh||")]    // ISO 639-1 Chinese
    [InlineData("EN||")]    // case-insensitive
    public void Id3LanguagePrefix_TwoLetter_IsStripped(string prefix)
    {
        var result = LrcParser.Parse($"{prefix}[ti:Demo]\n[00:01.00]hello");
        result.HasErrors.ShouldBeFalse();
        result.Document.Metadata.Title.ShouldBe("Demo");
        result.Document.Lines.Count.ShouldBe(1);
        result.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.Id3LanguagePrefixStripped);
    }

    [Fact]
    public void Id3LanguagePrefix_ThreeLetter_StillStripped_NotPartialMatchedAsTwoPlusJunk()
    {
        // Regression guard: "eng||" must consume 5 chars (3-letter + ||), NOT 4 chars
        // (2-letter "en" + leftover "g||" that would later be parsed as content).
        var result = LrcParser.Parse("eng||[ti:Demo]\n[00:01.00]hello");
        result.Document.Metadata.Title.ShouldBe("Demo");
        result.Document.Lines.Count.ShouldBe(1);
        ((ModernLrc.Model.LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("hello");
    }

    [Fact]
    public void Id3LanguagePrefix_OneLetter_NotStripped()
    {
        // "e||" — only one letter; must not strip.
        var result = LrcParser.Parse("e||[ti:Demo]\n[00:01.00]hi");
        result.Diagnostics.ShouldNotContain(d => d.Code == LrcDiagnosticIds.Id3LanguagePrefixStripped);
    }
}
