using ModernLrc.Model;

namespace ModernLrc.Internal;

/// <summary>Stable sort of a list of <see cref="LrcLine"/> by timestamp, used by the
/// scanner and the document builder. Ties resolve to original insertion order.
/// Includes a fast O(N) monotonicity check that skips the indexed-sort allocation when
/// the input is already in ascending timestamp order — the common case.</summary>
internal static class LrcLineStableSort
{
    /// <summary>Sort <paramref name="lines"/> by timestamp (stable). Returns a fresh array;
    /// <paramref name="reordered"/> reports whether any element changed position. Callers that
    /// emit a "lines reordered" diagnostic gate on that flag.</summary>
    public static LrcLine[] Sort(List<LrcLine> lines, out bool reordered)
    {
        // Fast monotonic check — most LRC files are already ordered, so we skip the
        // indexed-sort allocation in the common case.
        bool monotonic = true;
        for (int i = 1; i < lines.Count; i++)
        {
            if (lines[i].Timestamp.CompareTo(lines[i - 1].Timestamp) < 0)
            {
                monotonic = false;
                break;
            }
        }
        if (monotonic)
        {
            reordered = false;
            return lines.ToArray();
        }

        // Non-monotonic by definition ⇒ at least one element is out of order, so the sort
        // below must move at least one element. reordered is unconditionally true here.
        reordered = true;

        // Indexed sort: stable tie-break on original index keeps insertion order for equal timestamps.
        var indexed = new (int Index, LrcLine Line)[lines.Count];
        for (int i = 0; i < lines.Count; i++) indexed[i] = (i, lines[i]);
        Array.Sort(indexed, static (a, b) =>
        {
            int cmp = a.Line.Timestamp.CompareTo(b.Line.Timestamp);
            return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
        });

        var sorted = new LrcLine[indexed.Length];
        for (int i = 0; i < indexed.Length; i++) sorted[i] = indexed[i].Line;
        return sorted;
    }
}
