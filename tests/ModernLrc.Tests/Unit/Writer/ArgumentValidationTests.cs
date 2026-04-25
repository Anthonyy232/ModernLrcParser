using ModernLrc;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class ArgumentValidationTests
{
    [Fact]
    public void Write_NullDocument_Throws()
    {
        Should.Throw<ArgumentNullException>(() => LrcWriter.Write(null!));
    }

    [Fact]
    public void Write_NullStream_Throws()
    {
        var doc = LrcDocument.Empty;
        Should.Throw<ArgumentNullException>(() => LrcWriter.Write(doc, (Stream)null!));
    }

    [Fact]
    public void Write_UnwritableStream_Throws()
    {
        var doc = LrcDocument.Empty;
        var ms = new MemoryStream(new byte[10], writable: false);
        Should.Throw<ArgumentException>(() => LrcWriter.Write(doc, ms));
    }

    [Fact]
    public void WriteFile_EmptyPath_Throws()
    {
        Should.Throw<ArgumentException>(() => LrcWriter.WriteFile(LrcDocument.Empty, ""));
    }

    [Fact]
    public void Write_NullTextWriter_Throws()
    {
        Should.Throw<ArgumentNullException>(() => LrcWriter.Write(LrcDocument.Empty, (TextWriter)null!));
    }

    [Fact]
    public void Write_NullIBufferWriterChar_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            LrcWriter.Write(LrcDocument.Empty, (System.Buffers.IBufferWriter<char>)null!));
    }

    [Fact]
    public void Write_NullIBufferWriterByte_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            LrcWriter.Write(LrcDocument.Empty, (System.Buffers.IBufferWriter<byte>)null!));
    }

    [Fact]
    public void WriteFile_NullDocument_Throws()
    {
        Should.Throw<ArgumentNullException>(() => LrcWriter.WriteFile(null!, "x.lrc"));
    }

    [Fact]
    public void WriteFile_WhitespacePath_Throws()
    {
        Should.Throw<ArgumentException>(() => LrcWriter.WriteFile(LrcDocument.Empty, "   "));
    }

}
