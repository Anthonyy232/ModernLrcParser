using System.Buffers;
using System.Text;
using ModernLrc;

namespace ModernLrc.Tests.Unit.Writer;

/// <summary>The <see cref="IBufferWriter{T}"/> contract guarantees only "<i>at least</i>
/// the requested size" — a conforming implementation may return spans at exactly that size.
/// These tests pin the writer's behaviour against the contract minimum: a custom writer that
/// hands back single-element spans, exercising every internal buffer-lease boundary including
/// the boundary that would split a CRLF into two leases.</summary>
public sealed class BufferBoundaryTests
{
    /// <summary>Returns spans at exactly the requested size (or 1 when no hint), so every
    /// renderer append crosses a buffer boundary. Built-in writers like
    /// <see cref="ArrayBufferWriter{T}"/> over-allocate and would not exercise this path.</summary>
    private sealed class MinSpanBufferWriter<T> : IBufferWriter<T>
    {
        private T[] _buffer = [];
        private int _written;

        public void Advance(int count) => _written += count;

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            int requested = sizeHint <= 0 ? 1 : sizeHint;
            if (_buffer.Length - _written < requested)
            {
                int newSize = Math.Max(_buffer.Length * 2 + requested, _written + requested);
                var newBuf = new T[newSize];
                Array.Copy(_buffer, newBuf, _written);
                _buffer = newBuf;
            }
            return _buffer.AsMemory(_written, requested);
        }

        public Span<T> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

        public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _written);
    }

    [Fact]
    public void Write_ToCharBufferWriter_Crlf_NoTrailingNewline_OutputHasNoStrayCr()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();
        var options = new LrcWriteOptions { LineEnding = LrcLineEnding.Crlf, TrailingNewline = false };
        var bw = new MinSpanBufferWriter<char>();

        LrcWriter.Write(doc, bw, options);

        new string(bw.WrittenSpan).ShouldBe("[00:01.00]x");
    }

    [Fact]
    public void Write_ToUtf8BufferWriter_Crlf_NoTrailingNewline_OutputHasNoStrayCr()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();
        var options = new LrcWriteOptions { LineEnding = LrcLineEnding.Crlf, TrailingNewline = false };
        var bw = new MinSpanBufferWriter<byte>();

        LrcWriter.Write(doc, bw, options);

        Encoding.UTF8.GetString(bw.WrittenSpan).ShouldBe("[00:01.00]x");
    }

    [Fact]
    public void Write_ToCharBufferWriter_Crlf_WithTrailingNewline_OutputEndsWithCrlf()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();
        var options = new LrcWriteOptions { LineEnding = LrcLineEnding.Crlf, TrailingNewline = true };
        var bw = new MinSpanBufferWriter<char>();

        LrcWriter.Write(doc, bw, options);

        new string(bw.WrittenSpan).ShouldBe("[00:01.00]x\r\n");
    }

    [Fact]
    public void Write_ToUtf8BufferWriter_MetadataAndLyrics_PreservesBlankSeparator()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("T")
            .AddLine("00:01.00", "x")
            .Build();
        var options = new LrcWriteOptions { LineEnding = LrcLineEnding.Crlf, TrailingNewline = true };
        var bw = new MinSpanBufferWriter<byte>();

        LrcWriter.Write(doc, bw, options);

        Encoding.UTF8.GetString(bw.WrittenSpan).ShouldBe("[ti:T]\r\n\r\n[00:01.00]x\r\n");
    }

    [Fact]
    public void Write_ToCharBufferWriter_EmptyDocument_NoTrailingNewline_ProducesNoOutput()
    {
        var options = new LrcWriteOptions { TrailingNewline = false };
        var bw = new MinSpanBufferWriter<char>();

        LrcWriter.Write(LrcDocument.Empty, bw, options);

        new string(bw.WrittenSpan).ShouldBe(string.Empty);
    }

    [Fact]
    public void Write_ToCharBufferWriter_MetadataOnly_NoTrailingNewline_OutputEndsWithLastTag()
    {
        var doc = new LrcDocumentBuilder().WithTitle("T").Build();
        var options = new LrcWriteOptions { LineEnding = LrcLineEnding.Crlf, TrailingNewline = false };
        var bw = new MinSpanBufferWriter<char>();

        LrcWriter.Write(doc, bw, options);

        new string(bw.WrittenSpan).ShouldBe("[ti:T]");
    }
}
