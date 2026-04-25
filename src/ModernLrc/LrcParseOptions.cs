using System.Text;

namespace ModernLrc;

/// <summary>Parser strictness mode.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design", "CA1028:Enum storage should be Int32",
    Justification = "byte is intentional — option records are small and dense; saving 3 bytes per occurrence matters at scale.")]
public enum LrcStrictness : byte
{
    /// <summary>Collect all diagnostics; only catastrophic failures throw.</summary>
    Tolerant = 0,

    /// <summary>Throw <see cref="LrcParseException"/> on first Error-severity diagnostic.</summary>
    Strict = 1,
}

/// <summary>Configuration for a parse operation.</summary>
/// <example>
/// <code>
/// // Strict mode: throw on the first Error-severity diagnostic.
/// var strict = new LrcParseOptions { Strictness = LrcStrictness.Strict };
///
/// // Non-Unicode codepage: pass the encoding explicitly (statistical detection is out of scope).
/// var gbk = new LrcParseOptions { Encoding = Encoding.GetEncoding("GBK") };
///
/// // Suppress diagnostics entirely.
/// var quiet = new LrcParseOptions { MaxDiagnostics = 0 };
/// </code>
/// </example>
public sealed record LrcParseOptions
{
    /// <summary>Default options (Tolerant; UTF-8 fallback; 256 diagnostics; 4096-byte buffer).</summary>
    public static LrcParseOptions Default { get; } = new();

    /// <summary>Strictness mode (default Tolerant).</summary>
    public LrcStrictness Strictness { get; init; } = LrcStrictness.Tolerant;

    /// <summary>Force the input encoding for byte/stream inputs. <c>null</c> = auto-detect
    /// (BOM → UTF-8 validation → fallback). If your file is non-UTF-8 and has no BOM, you
    /// MUST set this — auto-detection will fail loud. For non-Unicode codepages, bring
    /// <c>System.Text.Encoding.CodePages</c> as a transitive dependency yourself.
    /// <para>Note: a recognised BOM (UTF-8, UTF-16 LE, UTF-16 BE) takes precedence over this
    /// property — matching <see cref="System.IO.StreamReader"/>'s behaviour. Strip the BOM
    /// from the byte array yourself if you need to force a different decoding.</para></summary>
    public Encoding? Encoding { get; init; }

    /// <summary>Used when auto-detect is inconclusive. <c>null</c> disables fallback
    /// (auto-detect failure throws).</summary>
    public Encoding? FallbackEncoding { get; init; } = System.Text.Encoding.UTF8;

    /// <summary>Cap on how many diagnostics will be emitted before <c>LRC0099</c> is raised
    /// once and the rest are suppressed. Must be ≥ 0. Default 256.</summary>
    public int MaxDiagnostics
    {
        get => _maxDiagnostics;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _maxDiagnostics = value;
        }
    }
    private readonly int _maxDiagnostics = 256;

    /// <summary>Threshold above which a timestamp triggers <c>LRC0070</c> (still parsed).</summary>
    public TimeSpan ImplausibleTimestampThreshold { get; init; } = TimeSpan.FromHours(24);

    /// <summary>Initial read buffer size for stream/file inputs (must be ≥ 64). Default 4096.</summary>
    public int ReadBufferSize
    {
        get => _readBufferSize;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 64);
            _readBufferSize = value;
        }
    }
    private readonly int _readBufferSize = 4096;
}
