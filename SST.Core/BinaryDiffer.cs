using SST.Core.Models;

namespace SST.Core;

public static class BinaryDiffer
{
    /// <summary>
    /// Computes contiguous regions where the two buffers differ. Buffers must be the same length.
    /// </summary>
    public static IReadOnlyList<BinaryDiffSegment> Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Buffers must be the same length for row-aligned diff.");

        var segments = new List<BinaryDiffSegment>();
        var i = 0;
        while (i < a.Length)
        {
            if (a[i] == b[i])
            {
                i++;
                continue;
            }

            var start = i;
            while (i < a.Length && a[i] != b[i])
                i++;

            segments.Add(new BinaryDiffSegment(start, i - start));
        }

        return MergeAdjacent(segments);
    }

    /// <summary>
    /// Compares the shared prefix of two buffers (useful when captures differ slightly in trailing padding).
    /// </summary>
    public static IReadOnlyList<BinaryDiffSegment> ComparePrefix(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var n = Math.Min(a.Length, b.Length);
        return Compare(a[..n], b[..n]);
    }

    private static List<BinaryDiffSegment> MergeAdjacent(List<BinaryDiffSegment> segments)
    {
        if (segments.Count <= 1)
            return segments;

        var merged = new List<BinaryDiffSegment>(segments.Count);
        var cur = segments[0];
        for (var idx = 1; idx < segments.Count; idx++)
        {
            var next = segments[idx];
            if (cur.StartOffset + cur.Length == next.StartOffset)
            {
                cur = new BinaryDiffSegment(cur.StartOffset, cur.Length + next.Length);
            }
            else
            {
                merged.Add(cur);
                cur = next;
            }
        }

        merged.Add(cur);
        return merged;
    }
}
