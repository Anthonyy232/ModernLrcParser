using System.Text;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class AsyncEntryTests
{
    [Fact]
    public async Task ParseAsync_Stream_Works()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("[00:01.00]async"));
        var result = await LrcParser.ParseAsync(ms, cancellationToken: TestContext.Current.CancellationToken);
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ParseAsync_MemoryStream_ConsumesFromCurrentPosition()
    {
        const string prefix = "[00:00.00]skip\n";
        const string payload = "[00:01.00]keep";
        var bytes = Encoding.UTF8.GetBytes(prefix + payload);
        using var ms = new MemoryStream(bytes, 0, bytes.Length, writable: false, publiclyVisible: true)
        {
            Position = Encoding.UTF8.GetByteCount(prefix),
        };

        var result = await LrcParser.ParseAsync(ms, cancellationToken: TestContext.Current.CancellationToken);

        ms.Position.ShouldBe(ms.Length);
        result.Document.Lines.Count.ShouldBe(1);
        ((LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("keep");
    }

    [Fact]
    public async Task ParseAsync_TextReader_Works()
    {
        using var reader = new StringReader("[00:01.00]reader");
        var result = await LrcParser.ParseAsync(reader, cancellationToken: TestContext.Current.CancellationToken);
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ParseAsync_Cancelled_Throws()
    {
        using var cts = new System.Threading.CancellationTokenSource();
        await cts.CancelAsync();
        using var reader = new StringReader("[00:01.00]x");
        await Should.ThrowAsync<OperationCanceledException>(
            () => LrcParser.ParseAsync(reader, cancellationToken: cts.Token).AsTask());
    }
}
