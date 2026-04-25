using System.Text;
using ModernLrc;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class FileEntryTests : IDisposable
{
    private readonly string _tmp = Path.Combine(
        Path.GetTempPath(), $"modernlrc-tests-{Guid.NewGuid():N}.lrc");

    public void Dispose()
    {
        if (File.Exists(_tmp)) File.Delete(_tmp);
    }

    [Fact]
    public void ParseFile_RoundTripsContent()
    {
        File.WriteAllText(_tmp, "[ti:Hello]\n[00:01.00]world", Encoding.UTF8);
        var result = LrcParser.ParseFile(_tmp);
        result.Document.Metadata.Title.ShouldBe("Hello");
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ParseFileAsync_RoundTripsContent()
    {
        await File.WriteAllTextAsync(_tmp, "[00:01.00]async-file", Encoding.UTF8,
            TestContext.Current.CancellationToken);
        var result = await LrcParser.ParseFileAsync(_tmp,
            cancellationToken: TestContext.Current.CancellationToken);
        result.Document.Lines.Count.ShouldBe(1);
    }
}
