using SST.Core.Models;

namespace SST.Core;

/// <summary>
/// Extensibility point for future structured decoders (per file kind or format version).
/// </summary>
public interface IDatDecoder
{
    IReadOnlyList<DatRecord> Decode(ReadOnlyMemory<byte> content, string fileName);
}

/// <summary>
/// Default decoder: no semantic keys; returns empty until reverse-engineering adds rules.
/// </summary>
public sealed class NoOpDatDecoder : IDatDecoder
{
    public IReadOnlyList<DatRecord> Decode(ReadOnlyMemory<byte> content, string fileName) => [];
}
