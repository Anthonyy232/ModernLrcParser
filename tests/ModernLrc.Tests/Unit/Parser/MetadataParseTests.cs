using ModernLrc;
using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class MetadataParseTests
{
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
    public void Tool_And_Re_Alias_LastWinsAcrossKeys()
    {
        var r = LrcParser.Parse("[re:Editor1]\n[tool:Editor2]\n[00:01.00]x");
        r.Document.Metadata.Tool.ShouldBe("Editor2");
        r.Diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.ConflictingMetadata);
    }
}
