using System.Collections.Immutable;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc;

public static partial class LrcWriter
{
    /// <summary>Pre-flight diagnostics (LRC0090 unrepresentable voice transitions). No rendering.</summary>
    public static ImmutableArray<LrcDiagnostic> ValidateForWrite(LrcDocument document, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= LrcWriteOptions.Default;

        if (!options.EmitVoiceMarkers) return ImmutableArray<LrcDiagnostic>.Empty;

        var builder = ImmutableArray.CreateBuilder<LrcDiagnostic>();
        var lastEmitted = LrcVoice.Default;
        for (int i = 0; i < document.Lines.Count; i++)
        {
            var voice = document.Lines[i].EffectiveVoice;
            if (voice != LrcVoice.Default)
            {
                lastEmitted = voice;
            }
            else if (lastEmitted != LrcVoice.Default)
            {
                // Walaoke has no "clear voice" marker, so this transition is unrepresentable.
                // lastEmitted stays as is: a re-parse would still see the previous voice.
                builder.Add(new LrcDiagnostic
                {
                    Severity = LrcDiagnosticSeverity.Warning,
                    Code = LrcDiagnosticIds.UnrepresentableVoiceTransition,
                    Line = i + 1,
                    Column = 1,
                    Length = 0,
                    Message = "Voice transition to Default cannot be emitted — Walaoke has no clear-voice marker.",
                });
            }
        }
        return builder.ToImmutable();
    }
}
