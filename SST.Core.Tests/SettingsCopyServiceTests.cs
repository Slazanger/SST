using SST.Core;
using SST.Core.Models;

namespace SST.Core.Tests;

public class SettingsCopyServiceTests
{
    [Fact]
    public void CopyWithBackups_copies_and_creates_backup()
    {
        var dir = Path.Combine(Path.GetTempPath(), "eve-copy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        var masterChar = Path.Combine(dir, "master_char.dat");
        var masterUser = Path.Combine(dir, "master_user.dat");
        File.WriteAllText(masterChar, "CHAR");
        File.WriteAllText(masterUser, "USER");

        var destChar = Path.Combine(dir, "core_char_1.dat");
        File.WriteAllText(destChar, "oldchar");

        var svc = new SettingsCopyService();
        var targets = new[]
        {
            new SettingsFileEntry(SettingsFileKind.Char, "1", Path.GetFileName(destChar), destChar, "srv",
                "settings_Default", DateTime.UtcNow),
        };

        var results = svc.CopyWithBackups(masterChar, masterUser, targets);

        Assert.Single(results);
        Assert.True(results[0].Succeeded);
        Assert.Equal("CHAR", File.ReadAllText(destChar));
        Assert.NotNull(results[0].BackupPath);
        Assert.True(File.Exists(results[0].BackupPath!));
        Assert.Equal("oldchar", File.ReadAllText(results[0].BackupPath!));
    }

    [Fact]
    public void CopyWithBackups_uses_user_master_for_user_targets()
    {
        var dir = Path.Combine(Path.GetTempPath(), "eve-copy2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        var masterChar = Path.Combine(dir, "master_char.dat");
        var masterUser = Path.Combine(dir, "master_user.dat");
        File.WriteAllText(masterChar, "CHAR");
        File.WriteAllText(masterUser, "USERNEW");

        var destUser = Path.Combine(dir, "core_user_9.dat");
        File.WriteAllText(destUser, "old");

        var svc = new SettingsCopyService();
        var targets = new[]
        {
            new SettingsFileEntry(SettingsFileKind.User, "9", Path.GetFileName(destUser), destUser, "srv",
                "settings_Default", DateTime.UtcNow),
        };

        var results = svc.CopyWithBackups(masterChar, masterUser, targets);

        Assert.True(results[0].Succeeded);
        Assert.Equal("USERNEW", File.ReadAllText(destUser));
    }
}
