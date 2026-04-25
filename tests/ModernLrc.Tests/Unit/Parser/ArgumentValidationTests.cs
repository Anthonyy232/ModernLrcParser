using System.IO;
using System.Threading.Tasks;
using ModernLrc;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class ArgumentValidationTests
{
    [Fact]
    public void Parse_NullString_Throws()
    {
        Should.Throw<ArgumentNullException>(() => LrcParser.Parse((string)null!));
    }

    [Fact]
    public void Parse_NullByteArray_Throws()
    {
        Should.Throw<ArgumentNullException>(() => LrcParser.Parse((byte[])null!));
    }

    [Fact]
    public void Parse_NullStream_Throws()
    {
        Should.Throw<ArgumentNullException>(() => LrcParser.Parse((Stream)null!));
    }

    [Fact]
    public void Parse_UnreadableStream_Throws()
    {
        // A closed MemoryStream is not readable.
        var ms = new MemoryStream();
        ms.Close();
        Should.Throw<ArgumentException>(() => LrcParser.Parse(ms));
    }

    [Fact]
    public void ParseFile_EmptyPath_Throws()
    {
        Should.Throw<ArgumentException>(() => LrcParser.ParseFile(""));
    }

    [Fact]
    public void ParseFile_Missing_PropagatesFileNotFound()
    {
        Should.Throw<FileNotFoundException>(() =>
            LrcParser.ParseFile("does-not-exist-anywhere.lrc"));
    }

}
