namespace ModernLrc.Model;

/// <summary>Walaoke voice marker carried on lyric lines.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design", "CA1028:Enum storage should be Int32",
    Justification = "byte is intentional — LrcVoice is widely embedded in records and arrays; saving 3 bytes per occurrence matters at scale.")]
public enum LrcVoice : byte
{
    /// <summary>No explicit voice; the line uses propagated state or the document default.</summary>
    Default = 0,

    /// <summary>Walaoke <c>M:</c> marker.</summary>
    Male = 1,

    /// <summary>Walaoke <c>F:</c> marker.</summary>
    Female = 2,

    /// <summary>Walaoke <c>D:</c> marker (duet / both).</summary>
    Duet = 3,
}
