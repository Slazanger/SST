using SST.Core;
using SST.Core.Models;

namespace SST.Core.Tests;

public class DatRecordEstimatorTests
{
    [Fact]
    public void FromDiffSegments_maps_each_segment_to_a_record()
    {
        var segments = new BinaryDiffSegment[]
        {
            new(10, 4),
            new(100, 2),
        };

        var records = DatRecordEstimator.FromDiffSegments(segments);

        Assert.Equal(2, records.Count);
        Assert.Equal(10, records[0].Offset);
        Assert.Equal(4, records[0].Length);
        Assert.Equal("ChangedRegion", records[0].KindGuess);
    }
}
