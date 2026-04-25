using ModernLrc;
using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class MetadataParseTests
{
    [Fact]
    public void Title_PopulatesStrongAccessor()
    {
        var result = LrcParser.Parse("[ti:Demo Song]\n[00:01.00]hi");
        result.Document.Metadata.Title.ShouldBe("Demo Song");
    }

    [Fact]
    public void Offset_PopulatesAsTimeSpan()
    {
        var result = LrcParser.Parse("[offset:-150]\n[00:01.00]hi");
        result.Document.Metadata.Offset.TotalMilliseconds.ShouldBe(-150);
    }

    [Fact]
    public void RawTags_PreservesEveryTag()
    {
        var result = LrcParser.Parse(
            "[ti:T]\n[ar:A]\n[custom:value]\n[00:01.00]hi");
        result.Document.Metadata.RawTags.Count.ShouldBe(3);
        result.Document.Metadata.RawTags[2].Key.ShouldBe("custom");
    }

    [Fact]
    public void RepeatedKey_LastWins()
    {
        var result = LrcParser.Parse("[ti:First]\n[ti:Last]\n[00:01.00]hi");
        result.Document.Metadata.Title.ShouldBe("Last");
        result.Document.Metadata.RawTags.Count.ShouldBe(2);
    }

    [Fact]
    public void InvalidOffset_EmitsWarning()
    {
        var result = LrcParser.Parse("[offset:not-a-number]\n[00:01.00]hi");
        result.HasErrors.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Code == "LRC0005");
    }

    [Fact]
    public void Tool_And_Re_Alias_LastWinsAcrossKeys()
    {
        // Both 're' and 'tool' map to LrcMetadata.Tool. Last-wins applies across the alias,
        // and a Conflicting-Metadata warning fires because the values differ.
        var r = LrcParser.Parse("[re:Editor1]\n[tool:Editor2]\n[00:01.00]x");
        r.Document.Metadata.Tool.ShouldBe("Editor2");
        r.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.ConflictingMetadata);
    }

    [Fact]
    public void Re_After_Tool_Alias_LastWins_ReverseOrder()
    {
        // Reverse direction — confirm the alias is symmetric.
        var r = LrcParser.Parse("[tool:Editor2]\n[re:Editor1]\n[00:01.00]x");
        r.Document.Metadata.Tool.ShouldBe("Editor1");
    }
}
