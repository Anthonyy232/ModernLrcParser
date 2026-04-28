using System.Text;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

/// <summary>Tests added to close gaps identified by a test-coverage audit:
/// roundtrip of multi-timestamp + voice + enhanced lines, BOM emission for UTF-8/UTF-16 BE,
/// WriteFile temp-file cleanup on failure (the moved-flag fix).</summary>
public sealed class AdditionalCoverageTests
{
    // -------------------------------------------------------------------------
    // Roundtrip — multi-timestamp + voice marker + enhanced words.
    // -------------------------------------------------------------------------

    [Fact]
    public void MultiTimestamp_VoicePropagation_Roundtrip()
    {
        // The 01s/05s multi-timestamp group is split by intervening 02s and 03s lines.
        // Under the single-timestamp model, fan-out + sort interleaves them — so the writer's
        // adjacent-only run-collapse cannot re-fold 01s and 05s together. This is an explicit
        // round-trip behavior change vs. the old multi-timestamp model: scattered groups become
        // independent lines. (Adjacent groups still round-trip identically; see SimpleWriteTests.)
        const string input =
            "[00:01.00][00:05.00]F: she sings\n" +
            "[00:02.00]still hers\n" +
            "[00:03.00]M: now him\n";

        var doc = LrcParser.Parse(input).Document;
        string written = LrcWriter.Write(doc);
        var reparsed = LrcParser.Parse(written).Document;

        // Sorted by timestamp: 01s "she sings", 02s "still hers", 03s "now him", 05s "she sings".
        reparsed.Lines.Count.ShouldBe(4);

        var l0 = (LrcPlainLine)reparsed.Lines[0];
        l0.Timestamp.TotalMilliseconds.ShouldBe(1_000);
        l0.Text.ShouldBe("she sings");
        l0.EffectiveVoice.ShouldBe(LrcVoice.Female);

        var l1 = (LrcPlainLine)reparsed.Lines[1];
        l1.Timestamp.TotalMilliseconds.ShouldBe(2_000);
        l1.Text.ShouldBe("still hers");
        l1.EffectiveVoice.ShouldBe(LrcVoice.Female);

        var l2 = (LrcPlainLine)reparsed.Lines[2];
        l2.Timestamp.TotalMilliseconds.ShouldBe(3_000);
        l2.Text.ShouldBe("now him");
        l2.EffectiveVoice.ShouldBe(LrcVoice.Male);

        var l3 = (LrcPlainLine)reparsed.Lines[3];
        l3.Timestamp.TotalMilliseconds.ShouldBe(5_000);
        l3.Text.ShouldBe("she sings");
        l3.EffectiveVoice.ShouldBe(LrcVoice.Female);
    }

    [Fact]
    public void EnhancedLine_WordTiming_Roundtrip()
    {
        const string input = "[00:01.00]<00:01.00>hello <00:01.50>world\n";
        var doc = LrcParser.Parse(input).Document;
        string written = LrcWriter.Write(doc);
        var reparsed = LrcParser.Parse(written).Document;

        var line = (LrcEnhancedLine)reparsed.Lines[0];
        line.Words.Count.ShouldBe(2);
        line.Words[0].Text.ShouldBe("hello ");
        line.Words[1].Text.ShouldBe("world");
        line.Words[1].Timestamp.TotalMilliseconds.ShouldBe(1_500);
    }

    // -------------------------------------------------------------------------
    // BOM emission — UTF-8 + Stream/IBufferWriter byte paths.
    // -------------------------------------------------------------------------

    [Fact]
    public void Utf8_EmitByteOrderMark_PrependsBomToStream()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "hello").Build();
        using var ms = new MemoryStream();
        var options = new LrcWriteOptions { EmitByteOrderMark = true, Encoding = Encoding.UTF8 };
        LrcWriter.Write(doc, ms, options);
        var bytes = ms.ToArray();
        bytes.Length.ShouldBeGreaterThanOrEqualTo(3);
        bytes[0].ShouldBe((byte)0xEF);
        bytes[1].ShouldBe((byte)0xBB);
        bytes[2].ShouldBe((byte)0xBF);
    }

    [Fact]
    public void Utf16Be_EmitByteOrderMark_PrependsBomToStream()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "hello").Build();
        using var ms = new MemoryStream();
        var options = new LrcWriteOptions { EmitByteOrderMark = true, Encoding = Encoding.BigEndianUnicode };
        LrcWriter.Write(doc, ms, options);
        var bytes = ms.ToArray();
        bytes.Length.ShouldBeGreaterThanOrEqualTo(2);
        bytes[0].ShouldBe((byte)0xFE);
        bytes[1].ShouldBe((byte)0xFF);
    }

    // -------------------------------------------------------------------------
    // Atomic file write — verify temp file is cleaned up after failure.
    // -------------------------------------------------------------------------

    [Fact]
    public void WriteFile_FailureBeforeMove_CleansUpTempFile()
    {
        // Use a path inside a non-existent directory to force failure during FileStream creation
        // (which happens BEFORE the temp file is created on disk — so really we want to fail mid-move).
        // Simpler approach: write to a directory we can list, count tmp files before/after.
        var dir = Path.Combine(Path.GetTempPath(), $"modernlrc-cleanup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            // Create a directory at the destination path so File.Move fails (cannot overwrite a dir).
            var dest = Path.Combine(dir, "destination");
            Directory.CreateDirectory(dest);

            var doc = new LrcDocumentBuilder().AddLine("00:01.00", "test").Build();
            int beforeCount = Directory.GetFiles(dir).Length;

            Should.Throw<Exception>(() => LrcWriter.WriteFile(doc, dest));

            int afterCount = Directory.GetFiles(dir).Length;
            afterCount.ShouldBe(beforeCount,
                "WriteFile failure path must not leave temp files behind.");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task WriteFileAsync_FailureBeforeMove_CleansUpTempFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modernlrc-cleanup-async-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var dest = Path.Combine(dir, "destination");
            Directory.CreateDirectory(dest);

            var doc = new LrcDocumentBuilder().AddLine("00:01.00", "test").Build();
            int beforeCount = Directory.GetFiles(dir).Length;

            await Should.ThrowAsync<Exception>(async () =>
                await LrcWriter.WriteFileAsync(doc, dest, cancellationToken: TestContext.Current.CancellationToken)
                    .ConfigureAwait(true)).ConfigureAwait(true);

            int afterCount = Directory.GetFiles(dir).Length;
            afterCount.ShouldBe(beforeCount,
                "WriteFileAsync failure path must not leave temp files behind.");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    // -------------------------------------------------------------------------
    // Alphabetical metadata ordering — raw tags sorted by key.
    // -------------------------------------------------------------------------

    [Fact]
    public void Alphabetical_RawTagsEmittedInKeyOrder()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("T")
            .Build();
        // Inject raw tags in non-alphabetical order via a fresh document (builder typically sorts).
        var docWithRaw = doc with
        {
            Metadata = doc.Metadata with
            {
                RawTags = System.Collections.Immutable.ImmutableArray.Create(
                    new LrcTag("zzz", "last"),
                    new LrcTag("aaa", "first"),
                    new LrcTag("mmm", "middle")
                ),
            },
        };

        var options = new LrcWriteOptions { MetadataOrdering = LrcMetadataOrdering.Alphabetical };
        string written = LrcWriter.Write(docWithRaw, options);

        int idxAaa = written.IndexOf("[aaa:", StringComparison.Ordinal);
        int idxMmm = written.IndexOf("[mmm:", StringComparison.Ordinal);
        int idxZzz = written.IndexOf("[zzz:", StringComparison.Ordinal);

        idxAaa.ShouldBeGreaterThan(-1);
        idxMmm.ShouldBeGreaterThan(idxAaa);
        idxZzz.ShouldBeGreaterThan(idxMmm);
    }

    // -------------------------------------------------------------------------
    // LrcDocumentBuilder — input validation and Build() idempotence.
    // -------------------------------------------------------------------------

    [Fact]
    public void Builder_WithRawTag_RejectsEmptyKey()
    {
        var b = new LrcDocumentBuilder();
        Should.Throw<ArgumentException>(() => b.WithRawTag("", "value"));
        Should.Throw<ArgumentException>(() => b.WithRawTag("   ", "value"));
        Should.Throw<ArgumentNullException>(() => b.WithRawTag(null!, "value"));
    }

    [Fact]
    public void Builder_Build_IsIdempotent_AndDoesNotMutateBuilder()
    {
        var b = new LrcDocumentBuilder()
            .WithTitle("T")
            .AddLine("00:02.00", "second")
            .AddLine("00:01.00", "first"); // out of order — exercises the indexed-sort path

        var doc1 = b.Build();
        var doc2 = b.Build();

        // Both materialisations must be sorted identically.
        doc1.Lines.Count.ShouldBe(2);
        doc2.Lines.Count.ShouldBe(2);
        ((LrcPlainLine)doc1.Lines[0]).Text.ShouldBe("first");
        ((LrcPlainLine)doc2.Lines[0]).Text.ShouldBe("first");
        // Builder retains insertion order — second Build sees the same lines.
        b.LineCount.ShouldBe(2);
    }

    [Fact]
    public void Builder_Build_SortsLines_RegardlessOfInsertionOrder()
    {
        var sortedDoc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "a")
            .AddLine("00:02.00", "b")
            .AddLine("00:03.00", "c")
            .Build();

        var reversedDoc = new LrcDocumentBuilder()
            .AddLine("00:03.00", "c")
            .AddLine("00:02.00", "b")
            .AddLine("00:01.00", "a")
            .Build();

        // Whatever order the caller hands us, Build's output is sorted by timestamp.
        for (int i = 0; i < sortedDoc.Lines.Count; i++)
        {
            ((LrcPlainLine)sortedDoc.Lines[i]).Text
                .ShouldBe(((LrcPlainLine)reversedDoc.Lines[i]).Text);
        }
    }

    // -------------------------------------------------------------------------
    // WriteAsync(Stream) UTF-8 fast path — must produce identical bytes to the sync path.
    // -------------------------------------------------------------------------

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance", "CA1849:Call async methods when in an async method",
        Justification = "Sync Write call is the intentional comparison baseline against WriteAsync.")]
    public async Task WriteAsync_Stream_Utf8_MatchesSyncOutput()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("Async")
            .AddLine("00:01.00", "alpha")
            .AddLine("00:02.00", "beta")
            .Build();

        using var syncMs = new MemoryStream();
        LrcWriter.Write(doc, syncMs);

        using var asyncMs = new MemoryStream();
        await LrcWriter.WriteAsync(doc, asyncMs, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        asyncMs.ToArray().ShouldBe(syncMs.ToArray());
    }

    [Fact]
    public void Alphabetical_RawTagsEmittedInKeyOrder_OnUtf8BufferWriter()
    {
        // Same contract as the chars/string path, exercised on the UTF-8 buffer writer overload.
        var doc = new LrcDocumentBuilder()
            .WithTitle("T")
            .Build();
        var docWithRaw = doc with
        {
            Metadata = doc.Metadata with
            {
                RawTags = System.Collections.Immutable.ImmutableArray.Create(
                    new LrcTag("zzz", "last"),
                    new LrcTag("aaa", "first")
                ),
            },
        };

        var buf = new System.Buffers.ArrayBufferWriter<byte>();
        var options = new LrcWriteOptions { MetadataOrdering = LrcMetadataOrdering.Alphabetical };
        LrcWriter.Write(docWithRaw, buf, options);
        var written = Encoding.UTF8.GetString(buf.WrittenSpan);

        written.IndexOf("[aaa:", StringComparison.Ordinal)
            .ShouldBeLessThan(written.IndexOf("[zzz:", StringComparison.Ordinal));
    }

    // -------------------------------------------------------------------------
    // Builder.AddEnhancedLine must reject default(LrcWord) regardless of overload
    // shape — its Text would otherwise be null and reach LrcEnhancedLine. The
    // single chokepoint in AddEnhancedLineCore enforces this for both the span
    // and IEnumerable entry shapes.
    // -------------------------------------------------------------------------

    [Fact]
    public void Builder_AddEnhancedLine_SpanOverload_RejectsDefaultLrcWordWithNullText()
    {
        var b = new LrcDocumentBuilder();
        var t = LrcTimestamp.Zero;
        // default(LrcWord) has Text == null (struct default).
        var words = new[] { default(LrcWord) };
        Should.Throw<ArgumentException>(() => b.AddEnhancedLine(t, (ReadOnlySpan<LrcWord>)words));
    }

    [Fact]
    public void Builder_AddEnhancedLineGroup_RejectsNullText()
    {
        var b = new LrcDocumentBuilder();
        var t = LrcTimestamp.Zero;
        var words = new[] { new LrcWord(t, "ok"), default };
        Should.Throw<ArgumentException>(() =>
            b.AddEnhancedLineGroup(new[] { t }.AsSpan(), (ReadOnlySpan<LrcWord>)words));
    }
}
