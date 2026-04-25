using System.Buffers;
using System.Text;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

/// <summary>Every <see cref="LrcWriter"/> sink — string, <see cref="IBufferWriter{T}"/>
/// of chars, and <see cref="IBufferWriter{T}"/> of UTF-8 bytes — must produce identical output
/// for every supported document shape and option combination. The string and chars sinks
/// share <see cref="Internal.LrcSpanRenderer.RenderToChars"/> internally, so the chars-vs-string
/// case is structural; the UTF-8 case is the real drift guard between the two render paths
/// inside <c>LrcSpanRenderer</c>.</summary>
public sealed class RendererParityTests
{
    public static TheoryData<string, LrcWriteOptions> OptionPermutations()
    {
        var data = new TheoryData<string, LrcWriteOptions>();
        foreach (var le in new[] { LrcLineEnding.Lf, LrcLineEnding.Crlf, LrcLineEnding.Cr })
        {
            foreach (var trailing in new[] { true, false })
            {
                foreach (var precision in new[] { LrcTimestampPrecision.Centiseconds, LrcTimestampPrecision.Milliseconds })
                {
                    foreach (var ordering in new[] { LrcMetadataOrdering.Canonical, LrcMetadataOrdering.Alphabetical })
                    {
                        foreach (var collapse in new[] { false, true })
                        {
                            data.Add(
                                $"{le}/{(trailing ? "trail" : "no-trail")}/{precision}/{ordering}/{(collapse ? "collapse" : "no-collapse")}",
                                new LrcWriteOptions
                                {
                                    LineEnding = le,
                                    TrailingNewline = trailing,
                                    TimestampPrecision = precision,
                                    MetadataOrdering = ordering,
                                    CollapseIdenticalLines = collapse,
                                });
                        }
                    }
                }
            }
        }
        return data;
    }

    private static LrcDocument BuildSampleDoc()
    {
        return new LrcDocumentBuilder()
            .WithTitle("Demo Song")
            .WithArtist("Tester")
            .WithAlbum("Album X")
            .WithLength(TimeSpan.FromSeconds(180))
            .WithOffset(TimeSpan.FromMilliseconds(-150))
            .WithRawTag("custom", "value")
            .WithRawTag("extra", "data")
            .AddLine("00:01.00", "first line")
            .AddLine("00:02.00", "first line") // collapsible
            .AddLine("00:03.00", "second line", LrcVoice.Female)
            .AddLine("00:04.00", "still hers")
            .AddLine("00:05.00", "now him", LrcVoice.Male)
            .AddEnhancedLine(
                LrcTimestamp.FromMilliseconds(6_000),
                [(LrcTimestamp.FromMilliseconds(6_000), "hello "),
                 (LrcTimestamp.FromMilliseconds(6_500), "world")])
            .Build();
    }

    [Theory]
    [MemberData(nameof(OptionPermutations))]
    public void StringSink_AndCharBufferWriterSink_ProduceIdenticalOutput(string label, LrcWriteOptions options)
    {
        _ = label; // for test display only
        var doc = BuildSampleDoc();

        var stringPath = LrcWriter.Write(doc, options);

        var bw = new ArrayBufferWriter<char>();
        LrcWriter.Write(doc, bw, options);
        var spanPath = new string(bw.WrittenSpan);

        spanPath.ShouldBe(stringPath);
    }

    [Theory]
    [MemberData(nameof(OptionPermutations))]
    public void StringSink_AndUtf8BufferWriterSink_ProduceIdenticalOutput(string label, LrcWriteOptions options)
    {
        _ = label;
        var doc = BuildSampleDoc();

        var stringPath = LrcWriter.Write(doc, options);

        var bw = new ArrayBufferWriter<byte>();
        LrcWriter.Write(doc, bw, options);
        var spanPath = Encoding.UTF8.GetString(bw.WrittenSpan);

        spanPath.ShouldBe(stringPath);
    }

    [Fact]
    public void TrailingNewlineFalse_PreservesUserSuppliedTrailingNewlineInText_StringSink()
    {
        // The renderer never strips trailing \r/\n from user content; TrailingNewline=false
        // only declines to ADD an extra line ending after the final line.
        var doc = new LrcDocumentBuilder()
            .AddLine(LrcTimestamp.Zero, "foo\n")
            .Build();
        var options = new LrcWriteOptions { TrailingNewline = false };

        LrcWriter.Write(doc, options).ShouldBe("[00:00.00]foo\n");
    }

    [Fact]
    public void TrailingNewlineFalse_PreservesUserSuppliedTrailingNewlineInText_BufferWriterSink()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine(LrcTimestamp.Zero, "foo\n")
            .Build();
        var options = new LrcWriteOptions { TrailingNewline = false };
        var bw = new ArrayBufferWriter<char>();
        LrcWriter.Write(doc, bw, options);

        new string(bw.WrittenSpan).ShouldBe("[00:00.00]foo\n");
    }
}
