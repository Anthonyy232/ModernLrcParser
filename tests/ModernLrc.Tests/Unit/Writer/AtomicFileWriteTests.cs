using ModernLrc;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class AtomicFileWriteTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"modernlrc-write-{Guid.NewGuid():N}.lrc");

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public void WriteFile_CreatesFileAtomically()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();
        LrcWriter.WriteFile(doc, _path);
        File.ReadAllText(_path).ShouldBe("[00:01.00]x\n");
    }

    [Fact]
    public void WriteFile_OverwritesExisting()
    {
        File.WriteAllText(_path, "OLD");
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "new").Build();
        LrcWriter.WriteFile(doc, _path);
        File.ReadAllText(_path).ShouldBe("[00:01.00]new\n");
    }

    [Fact]
    public void WriteFile_NoTmpFileLeftBehind_OnSuccess()
    {
        // Use a fresh empty directory so we can verify the only file present after a
        // successful WriteFile is the destination — i.e. no scratch/temp file remained.
        // The shared temp dir would be too noisy to assert against.
        var dir = Path.Combine(Path.GetTempPath(), $"modernlrc-success-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var dest = Path.Combine(dir, "song.lrc");
            var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();

            LrcWriter.WriteFile(doc, dest);

            var files = Directory.GetFiles(dir);
            files.Length.ShouldBe(1, "WriteFile success path must leave only the destination file behind.");
            files[0].ShouldBe(dest);
            File.ReadAllText(dest).ShouldBe("[00:01.00]x\n");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
