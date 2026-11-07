# Playback

The extension methods in
[`LrcDocumentExtensions`](xref:ModernLrc.Model.LrcDocumentExtensions) look up lines by
playback position, for example to show the current line of a song while it plays.

## Offset

Line timestamps are kept as written. `[offset:N]` is stored in
[`LrcMetadata.Offset`](xref:ModernLrc.Model.LrcMetadata.Offset), so writing the document
gives back the same tag. The lookup methods apply it for you: a line's effective time is
its timestamp plus `Offset`.

```csharp
TimeSpan effective = doc.GetEffectiveTime(line.Timestamp);
```

`GetEffectiveTime` returns a `TimeSpan` because a negative offset can take it below
zero, which an `LrcTimestamp` can't represent.

## Current line

[`FindLineAt`](xref:ModernLrc.Model.LrcDocumentExtensions.FindLineAt*) returns the last
line whose effective time is at or before the position, so between two lines you get
the earlier one. It returns `null` before the first line or when the document has no
lines. It is a binary search, so calling it every frame is fine.

```csharp
LrcLine? current = doc.FindLineAt(player.Position);
string text = current?.GetText() ?? "";
```

## Current word

On an enhanced line, the current word is the last one whose effective time is at or
before the position:

```csharp
if (doc.FindLineAt(player.Position) is LrcEnhancedLine enhanced)
{
    LrcWord? currentWord = null;
    foreach (var word in enhanced.Words)
    {
        if (doc.GetEffectiveTime(word.Timestamp) > player.Position) break;
        currentWord = word;
    }
    // Highlight currentWord.
}
```

## Upcoming lines

[`LinesInRange`](xref:ModernLrc.Model.LrcDocumentExtensions.LinesInRange*) returns the
lines whose effective time is in `[start, end)`, in order:

```csharp
foreach (var line in doc.LinesInRange(player.Position, player.Position + TimeSpan.FromSeconds(10)))
    Console.WriteLine(line.GetText());
```

## Voice

Each line's [`EffectiveVoice`](xref:ModernLrc.Model.LrcLine.EffectiveVoice) is already
resolved: a Walaoke `M: `, `F: ` or `D: ` marker applies to its line and every line
after it until the next marker.

```csharp
string color = doc.FindLineAt(player.Position)?.EffectiveVoice switch
{
    LrcVoice.Male => "blue",
    LrcVoice.Female => "pink",
    LrcVoice.Duet => "gold",
    _ => "white",
};
```
