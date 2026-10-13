using ModernLrc;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class AtomicFileWriteTests : IDisposable
{
    private readonly string _dir = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), $"modernlrc-write-{Guid.NewGuid():N}")).FullName;

    private readonly LrcDocument _doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void WriteFile_OverwritesExisting_AndLeavesNoTempFile()
    {
        var dest = Path.Combine(_dir, "song.lrc");
        File.WriteAllText(dest, "OLD");

        LrcWriter.WriteFile(_doc, dest);

        File.ReadAllText(dest).ShouldBe("[00:01.00]x\n");
        Directory.GetFiles(_dir).ShouldBe([dest]);
    }

    [Fact]
    public void WriteFile_FailedMove_CleansUpTempFile()
    {
        // A directory at the destination makes File.Move fail after the temp file is written.
        var dest = Directory.CreateDirectory(Path.Combine(_dir, "destination")).FullName;

        Record.Exception(() => LrcWriter.WriteFile(_doc, dest)).ShouldNotBeNull();

        Directory.GetFiles(_dir).ShouldBeEmpty();
    }

    [Fact]
    public async Task WriteFileAsync_FailedMove_CleansUpTempFile()
    {
        var dest = Directory.CreateDirectory(Path.Combine(_dir, "destination")).FullName;

        (await Record.ExceptionAsync(() =>
            LrcWriter.WriteFileAsync(_doc, dest, cancellationToken: TestContext.Current.CancellationToken).AsTask()))
            .ShouldNotBeNull();

        Directory.GetFiles(_dir).ShouldBeEmpty();
    }

    [Fact]
    public void WriteFile_WhitespacePath_Throws()
    {
        Should.Throw<ArgumentException>(() => LrcWriter.WriteFile(_doc, "   "));
    }
}
