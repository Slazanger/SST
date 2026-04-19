namespace SST.Core.Models;

/// <summary>
/// Heuristic record boundary for binary inspection; not an official EVE schema type.
/// </summary>
public sealed record DatRecord(long Offset, int Length, string KindGuess, string? Note = null);
