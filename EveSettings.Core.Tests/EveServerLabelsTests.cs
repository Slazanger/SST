using EveSettings.Core;

namespace EveSettings.Core.Tests;

public class EveServerLabelsTests
{
    [Theory]
    [InlineData("c_tranquility", "Tranquility (TQ)")]
    [InlineData("_tq_tranquility", "Tranquility (TQ)")]
    [InlineData("C_TRANQUILITY", "Tranquility (TQ)")]
    [InlineData("c_singularity", "Singularity (Sisi)")]
    [InlineData("c_serenity", "Serenity")]
    [InlineData("c_infinity", "Infinity")]
    [InlineData("c_duality", "Duality")]
    [InlineData("c_thunderdome", "Thunderdome")]
    [InlineData("custom_shard", "Unknown shard (custom_shard)")]
    public void GetDisplayLabel_maps_known_folders(string folder, string expected) =>
        Assert.Equal(expected, EveServerLabels.GetDisplayLabel(folder));

    [Fact]
    public void GetDisplayLabel_empty_is_unknown() =>
        Assert.Equal("Unknown shard", EveServerLabels.GetDisplayLabel(""));
}
