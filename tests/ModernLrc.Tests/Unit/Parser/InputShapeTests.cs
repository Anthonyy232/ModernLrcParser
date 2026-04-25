using System.Text;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

/// <summary>Behaviour coverage for the input-shape overloads of
/// <see cref="LrcParser.Parse(System.ReadOnlySpan{char}, LrcParseOptions?)"/>:
/// <see cref="ReadOnlyMemory{T}"/> of char, a synchronous <see cref="TextReader"/>,
/// and a synchronous file-backed <see cref="Stream"/> (a stream type that doesn't expose
/// a buffer for the fast path).</summary>
public sealed class InputShapeTests
{
    [Fact]
    public void Parse_FromReadOnlyMemoryChar_ProducesExpectedDocument()
    {
        ReadOnlyMemory<char> memory = "[00:01.00]hello".AsMemory();
        var result = LrcParser.Parse(memory);
        result.HasErrors.ShouldBeFalse();
        result.Document.Lines.Count.ShouldBe(1);
        ((LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("hello");
    }

    [Fact]
    public void Parse_FromTextReader_Sync_ReadsAllContent()
    {
        using var reader = new StringReader("[ti:Sync]\n[00:01.00]first");
        var result = LrcParser.Parse(reader);
        result.Document.Metadata.Title.ShouldBe("Sync");
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void Parse_FromTextReader_Sync_NullReader_Throws()
    {
        Should.Throw<ArgumentNullException>(() => LrcParser.Parse((TextReader)null!));
    }

    [Fact]
    public void Parse_FromFileBackedStream_Sync_ProducesExpectedDocument()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"modernlrc-sync-stream-{Guid.NewGuid():N}.lrc");
        try
        {
            File.WriteAllText(tmp, "[00:01.00]from-file-stream", Encoding.UTF8);
            using var fs = File.OpenRead(tmp);
            var result = LrcParser.Parse(fs);
            result.Document.Lines.Count.ShouldBe(1);
            ((LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("from-file-stream");
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
