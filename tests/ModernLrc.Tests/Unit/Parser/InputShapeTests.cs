using System.Text;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class InputShapeTests
{
    [Fact]
    public void Parse_FromTextReader_Sync_ReadsAllContent()
    {
        using var reader = new StringReader("[ti:Sync]\n[00:01.00]first");
        var result = LrcParser.Parse(reader);
        result.Document.Metadata.Title.ShouldBe("Sync");
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void Parse_FromMemoryStream_Sync_ConsumesFromCurrentPosition()
    {
        const string prefix = "[00:00.00]skip\n";
        const string payload = "[00:01.00]keep";
        var bytes = Encoding.UTF8.GetBytes(prefix + payload);
        using var ms = new MemoryStream(bytes, 0, bytes.Length, writable: false, publiclyVisible: true)
        {
            Position = Encoding.UTF8.GetByteCount(prefix),
        };

        var result = LrcParser.Parse(ms);

        ms.Position.ShouldBe(ms.Length);
        result.Document.Lines.Count.ShouldBe(1);
        ((LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("keep");
    }
}
