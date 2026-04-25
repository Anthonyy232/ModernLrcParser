using System.Text;
using ModernLrc;

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
