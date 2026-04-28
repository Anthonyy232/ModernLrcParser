# Playback

`LrcDocumentExtensions` exposes the playback hot path — the methods a karaoke /
lyric-display consumer typically calls per video frame.

## The offset model

Documents store `[offset:N]` verbatim in
[`LrcMetadata.Offset`](xref:ModernLrc.Model.LrcMetadata.Offset). Line timestamps
are **not mutated** at parse time. This preserves round-trip fidelity — re-writing
the document emits the original `[offset:N]` tag rather than collapsing it
into mutated timestamps.

When you query the document for "what's playing now", apply the offset on the way
out:

```csharp
TimeSpan effective = doc.GetEffectiveTime(line.Timestamp);
LrcLine? current = doc.FindLineAt(player.Position);   // already factors offset
```

[`GetEffectiveTime`](xref:ModernLrc.Model.LrcDocumentExtensions.GetEffectiveTime*)
returns `TimeSpan` (signed) rather than `LrcTimestamp` because a large negative
offset can shift past zero — `LrcTimestamp` cannot represent that.

## Find the currently-singing line

```csharp
// O(log n) binary search; null if Lines is empty or position precedes every line.
LrcLine? current = doc.FindLineAt(player.Position);

string text = current switch
{
    LrcPlainLine plain    => plain.Text,
    LrcEnhancedLine enh   => enh.GetText(),    // joins all words
    null                  => "",
    _                     => current.GetText(),
};
```

[`FindLineAt`](xref:ModernLrc.Model.LrcDocumentExtensions.FindLineAt*) returns the
*greatest* line whose effective timestamp is ≤ the position. So it returns
the line that's currently being sung, even if the position is between two lines.

## Highlight the current word in an enhanced line

```csharp
if (doc.FindLineAt(player.Position) is LrcEnhancedLine enhanced)
{
    var posTs = LrcTimestamp.FromTimeSpan(player.Position - doc.Metadata.Offset);
    LrcWord? currentWord = null;
    foreach (var word in enhanced.Words)
        if (word.Timestamp <= posTs) currentWord = word; else break;
    // Render currentWord highlighted, others plain.
}
```

## Show a window of upcoming lines

```csharp
var window = doc.LinesInRange(
    start: player.Position,
    end:   player.Position + TimeSpan.FromSeconds(10));

foreach (var line in window)
    Console.WriteLine(line.GetText());
```

[`LinesInRange`](xref:ModernLrc.Model.LrcDocumentExtensions.LinesInRange*) is a
linear scan (yields lazily). It's cheap enough for per-frame use on documents up
to thousands of lines.

## Voice state

Each line carries [`EffectiveVoice`](xref:ModernLrc.Model.LrcLine.EffectiveVoice)
already resolved — the parser propagates the last explicit `M:` / `F:` / `D:`
marker forward across subsequent untagged lines. You don't need to track voice
state yourself.

```csharp
var line = (LrcPlainLine)doc.FindLineAt(player.Position)!;
display.SetSpeakerColor(line.EffectiveVoice switch
{
    LrcVoice.Male   => Brushes.SkyBlue,
    LrcVoice.Female => Brushes.Pink,
    LrcVoice.Duet   => Brushes.Gold,
    _               => Brushes.White,
});
```

## Performance notes

- `FindLineAt` is O(log n) — call it freely (60 fps × thousands of lines = no problem).
- `LinesInRange` is O(n) but allocation-free per yielded item; the iterator state
  machine is one allocation per call.
- `GetText` on an enhanced line uses `string.Create` to build the result with a
  single allocation sized to the exact total length.
