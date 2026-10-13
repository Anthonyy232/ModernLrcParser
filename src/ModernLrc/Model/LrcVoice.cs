namespace ModernLrc.Model;

/// <summary>Walaoke voice marker carried on lyric lines.</summary>
public enum LrcVoice
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
