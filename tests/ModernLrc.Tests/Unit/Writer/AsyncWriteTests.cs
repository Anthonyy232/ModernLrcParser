using System.Text;
using ModernLrc;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class AsyncWriteTests
{
    [Fact]
    public async Task WriteAsync_TextWriter_Works()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "async").Build();
        using var sw = new StringWriter();
        await LrcWriter.WriteAsync(doc, sw, cancellationToken: TestContext.Current.CancellationToken);
        sw.ToString().ShouldBe("[00:01.00]async\n");
    }

    [Fact]
    public async Task WriteAsync_Stream_Works()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "stream").Build();
        using var ms = new MemoryStream();
        await LrcWriter.WriteAsync(doc, ms, cancellationToken: TestContext.Current.CancellationToken);
        Encoding.UTF8.GetString(ms.ToArray()).ShouldBe("[00:01.00]stream\n");
    }

    [Fact]
    public async Task WriteFileAsync_Works()
    {
        var path = Path.Combine(Path.GetTempPath(), $"modernlrc-async-{Guid.NewGuid():N}.lrc");
        try
        {
            var doc = new LrcDocumentBuilder().AddLine("00:01.00", "file-async").Build();
            await LrcWriter.WriteFileAsync(doc, path, cancellationToken: TestContext.Current.CancellationToken);
            (await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).ShouldBe("[00:01.00]file-async\n");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
