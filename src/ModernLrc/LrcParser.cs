using System.Diagnostics.CodeAnalysis;

namespace ModernLrc;

/// <summary>Entry-point for parsing LRC text. All overloads converge on the canonical
/// <see cref="Parse(ReadOnlySpan{char}, LrcParseOptions?)"/>.</summary>
/// <remarks>
/// <para>The parser is tolerant by default — every recoverable issue is recorded as a
/// <see cref="Diagnostics.LrcDiagnostic"/> on <see cref="LrcParseResult.Diagnostics"/>
/// and parsing continues. Set <see cref="LrcParseOptions.Strictness"/> to
/// <see cref="LrcStrictness.Strict"/> to throw <see cref="LrcParseException"/> on the
/// first Error-severity diagnostic instead.</para>
/// <para>Byte / stream / file overloads run a BOM → caller-encoding → UTF-8 → fallback
/// pipeline before scanning. See the <c>articles/encoding-pipeline</c> guide.</para>
/// </remarks>
/// <example>
/// <code>
/// using ModernLrc;
///
/// var result = LrcParser.Parse("[ti:Demo]\n[00:01.00]hello\n");
/// Console.WriteLine(result.Document.Metadata.Title);   // "Demo"
/// Console.WriteLine(result.Document.Lines.Count);      // 1
/// foreach (var d in result.Diagnostics)
///     Console.WriteLine($"{d.Code} {d.Severity} L{d.Line}:C{d.Column} — {d.Message}");
/// </code>
/// </example>
public static partial class LrcParser
{
    /// <summary>Canonical static parse. All paths converge here.</summary>
    /// <param name="input">Source text. Whitespace and line endings are preserved verbatim
    /// in <see cref="Model.LrcPlainLine.Text"/>.</param>
    /// <param name="options">Parser options. Defaults to <see cref="LrcParseOptions.Default"/>
    /// (Tolerant strictness, 256-diagnostic cap).</param>
    /// <returns>The parsed result, including any collected diagnostics.</returns>
    /// <exception cref="LrcParseException">Strict mode encountered an Error-severity diagnostic.</exception>
    public static LrcParseResult Parse(ReadOnlySpan<char> input, LrcParseOptions? options = null)
    {
        options ??= LrcParseOptions.Default;
        var emitter = new Internal.LrcDiagnosticEmitter(options);
        var scanner = new Internal.LrcScanner(input, options, emitter);
        var result = scanner.Run();
        if (emitter.IsStrict && emitter.FirstError is not null)
        {
            throw new LrcParseException(
                "Strict-mode parse failed on first Error diagnostic.",
                result, emitter.FirstError, filePath: null);
        }
        return result;
    }

    /// <summary>Parse a string. Equivalent to <c>Parse(input.AsSpan(), options)</c>.</summary>
    /// <param name="input">Source text.</param>
    /// <param name="options">Parser options. Defaults to <see cref="LrcParseOptions.Default"/>.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
    /// <exception cref="LrcParseException">Strict mode encountered an Error-severity diagnostic.</exception>
    [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
    public static LrcParseResult Parse([StringSyntax("Lrc")] string input, LrcParseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Parse(input.AsSpan(), options);
    }

    /// <summary>Parse a <see cref="ReadOnlyMemory{T}"/> of chars.</summary>
    /// <param name="input">Source memory region.</param>
    /// <param name="options">Parser options.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="LrcParseException">Strict mode encountered an Error-severity diagnostic.</exception>
    public static LrcParseResult Parse(ReadOnlyMemory<char> input, LrcParseOptions? options = null)
        => Parse(input.Span, options);

    /// <summary>Parse a <see cref="TextReader"/> by reading it to end first.
    /// For large inputs prefer <see cref="ParseAsync(TextReader, LrcParseOptions?, CancellationToken)"/>.</summary>
    /// <param name="reader">Source reader; consumed to end.</param>
    /// <param name="options">Parser options.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
    /// <exception cref="LrcParseException">Strict mode encountered an Error-severity diagnostic.</exception>
    public static LrcParseResult Parse(TextReader reader, LrcParseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return Parse(reader.ReadToEnd().AsSpan(), options);
    }

    // Parse(ReadOnlySpan<byte>, ...), Parse(byte[]), Parse(Stream), ParseFile
    // are in LrcParser.ByteInput.cs. ParseAsync / ParseFileAsync are in LrcParser.Async.cs.

    /// <summary>BCL <c>TryParse</c> convention. Returns <c>false</c> if any
    /// Error-severity diagnostic was emitted; diagnostics are otherwise discarded.</summary>
    /// <param name="input">Source text.</param>
    /// <param name="document">Parsed document on success; null on failure.</param>
    /// <returns><c>true</c> when parsing produced a usable document with no Error diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
    /// <example>
    /// <code>
    /// if (LrcParser.TryParse("[00:01.00]hi", out var doc))
    ///     Console.WriteLine(doc.Lines.Count);
    /// </code>
    /// </example>
    public static bool TryParse([StringSyntax("Lrc")] string input, [NotNullWhen(true)] out LrcDocument? document)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = Parse(input.AsSpan());
        if (result.HasErrors) { document = null; return false; }
        document = result.Document;
        return true;
    }

    /// <summary>BCL convention with span input.</summary>
    /// <param name="input">Source span.</param>
    /// <param name="document">Parsed document on success; null on failure.</param>
    /// <returns><c>true</c> when parsing produced a usable document with no Error diagnostics.</returns>
    public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out LrcDocument? document)
    {
        var result = Parse(input);
        if (result.HasErrors) { document = null; return false; }
        document = result.Document;
        return true;
    }

    /// <summary>BCL convention with span input plus diagnostic capture.</summary>
    /// <param name="input">Source span.</param>
    /// <param name="document">Parsed document on success; null on failure.</param>
    /// <param name="diagnostics">All diagnostics emitted during the attempt, regardless of outcome.</param>
    /// <returns><c>true</c> when parsing produced a usable document with no Error diagnostics.</returns>
    public static bool TryParse(
        ReadOnlySpan<char> input,
        [NotNullWhen(true)] out LrcDocument? document,
        out System.Collections.Immutable.ImmutableArray<Diagnostics.LrcDiagnostic> diagnostics)
    {
        var result = Parse(input);
        diagnostics = result.Diagnostics;
        if (result.HasErrors) { document = null; return false; }
        document = result.Document;
        return true;
    }
}
