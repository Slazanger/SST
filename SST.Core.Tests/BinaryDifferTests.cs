using SST.Core;

namespace SST.Core.Tests;

public class BinaryDifferTests
{
    [Fact]
    public void Compare_finds_single_changed_span()
    {
        var a = new byte[] { 1, 2, 3, 4, 5 };
        var b = new byte[] { 1, 2, 9, 9, 5 };

        var segs = BinaryDiffer.Compare(a, b);

        Assert.Single(segs);
        Assert.Equal(2, segs[0].StartOffset);
        Assert.Equal(2, segs[0].Length);
    }
}
