namespace ModernLrc.Model;

/// <summary>One word in an Enhanced LRC line. <see cref="Text"/> includes any trailing
/// whitespace up to the next <c>&lt;</c> marker or line end so that
/// <c>string.Concat(words.Select(w =&gt; w.Text))</c> reproduces the line text exactly.</summary>
/// <param name="Timestamp">Word-onset time.</param>
/// <param name="Text">Verbatim text including trailing whitespace.</param>
public readonly record struct LrcWord(LrcTimestamp Timestamp, string Text);
