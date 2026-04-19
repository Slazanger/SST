using EveSettings.Core;

namespace EveSettings.Core.Tests;

public class SettingsScannerTests
{
    [Fact]
    public void Scan_finds_char_and_user_files_under_server_profile_layout()
    {
        var root = Path.Combine(Path.GetTempPath(), "eve-scan-" + Guid.NewGuid().ToString("N"));
        var profile = Path.Combine(root, "c_tranquility", "settings_Default");
        Directory.CreateDirectory(profile);
        File.WriteAllText(Path.Combine(profile, "core_char_111.dat"), "x");
        File.WriteAllText(Path.Combine(profile, "core_user_222.dat"), "y");

        var result = SettingsScanner.Scan(root);

        Assert.Equal(2, result.Files.Count);
        Assert.Contains(result.Files, f => f.Kind == Models.SettingsFileKind.Char && f.Id == "111");
        Assert.Contains(result.Files, f => f.Kind == Models.SettingsFileKind.User && f.Id == "222");
    }

    [Fact]
    public void Scan_server_root_directly_when_it_contains_settings_folders()
    {
        var root = Path.Combine(Path.GetTempPath(), "eve-scan2-" + Guid.NewGuid().ToString("N"));
        var profile = Path.Combine(root, "settings_Default");
        Directory.CreateDirectory(profile);
        File.WriteAllText(Path.Combine(profile, "core_char_5.dat"), "a");

        var result = SettingsScanner.Scan(root);

        Assert.Single(result.Files);
        Assert.Equal("5", result.Files[0].Id);
    }
}
