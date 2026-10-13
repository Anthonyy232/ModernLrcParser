using ModernLrc.Model;

namespace ModernLrc.Internal;

/// <summary>Stable sort of a list of <see cref="LrcLine"/> by timestamp, used by the
/// scanner and the document builder. Ties resolve to original insertion order.</summary>
internal static class LrcLineStableSort
{
    /// <summary>Sort <paramref name="lines"/> by timestamp (stable) into a fresh array;
    /// <paramref name="reordered"/> reports whether the input was out of order.</summary>
    public static LrcLine[] Sort(List<LrcLine> lines, out bool reordered)
    {
        reordered = false;
        for (int i = 1; i < lines.Count && !reordered; i++)
            reordered = lines[i].Timestamp < lines[i - 1].Timestamp;

        return reordered ? lines.OrderBy(static l => l.Timestamp).ToArray() : lines.ToArray();
    }
}
