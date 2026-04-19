using EveSettings.Core.Models;

namespace EveSettings.Core;

/// <summary>
/// Builds coarse <see cref="DatRecord"/> entries from diff segments (scaffold for future decoding).
/// </summary>
public static class DatRecordEstimator
{
    public static IReadOnlyList<DatRecord> FromDiffSegments(IReadOnlyList<BinaryDiffSegment> segments)
    {
        return segments.Select(s =>
                new DatRecord(s.StartOffset, s.Length, "ChangedRegion",
                    "Heuristic boundary from binary diff; not an official EVE record type."))
            .ToList();
    }
}
