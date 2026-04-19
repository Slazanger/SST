using SST.Core;

namespace SST.Core.Tests;

public class BinaryDifferPrefixTests
{
    [Fact]
    public void ComparePrefix_ignores_trailing_bytes_when_lengths_differ()
    {
        var a = new byte[] { 1, 2, 3, 4, 5 };
        var b = new byte[] { 1, 2, 3, 9 };

        var segs = BinaryDiffer.ComparePrefix(a, b);

        Assert.Single(segs);
        Assert.Equal(3, segs[0].StartOffset);
        Assert.Equal(1, segs[0].Length);
    }
}
