using System.Text.RegularExpressions;
using SST.Core.Models;

namespace SST.Core;

public static partial class SettingsScanner
{
    [GeneratedRegex(@"^core_char_(\d+)\.dat$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CharFileRegex();

    [GeneratedRegex(@"^core_user_(\d+)\.dat$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UserFileRegex();

    /// <summary>
    /// Scans either the top-level <c>CCP/EVE</c> folder or a single server folder (e.g. <c>c_tranquility</c>).
    /// </summary>
    public static ScanResult Scan(string rootPath)
    {
        var result = new ScanResult();
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            result.Warnings.Add("The selected folder does not exist or is not accessible.");
            return result;
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));

        if (DirectoryContainsProfileFolders(root))
        {
            ScanServerFolder(root, Path.GetFileName(root) ?? root, result);
            return result;
        }

        foreach (var serverDir in Directory.EnumerateDirectories(root))
        {
            var serverName = Path.GetFileName(serverDir) ?? serverDir;
            ScanServerFolder(serverDir, serverName, result);
        }

        if (result.Files.Count == 0)
            result.Warnings.Add("No core_char_*.dat or core_user_*.dat files were found under the selected folder.");

        return result;
    }

    private static bool DirectoryContainsProfileFolders(string path)
    {
        foreach (var dir in Directory.EnumerateDirectories(path))
        {
            var name = Path.GetFileName(dir.AsSpan());
            if (name.StartsWith("settings_", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void ScanServerFolder(string serverDir, string serverFolderName, ScanResult result)
    {
        foreach (var profileDir in Directory.EnumerateDirectories(serverDir))
        {
            var profileName = Path.GetFileName(profileDir);
            if (profileName is null ||
                !profileName.StartsWith("settings_", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var file in Directory.EnumerateFiles(profileDir))
            {
                var fileName = Path.GetFileName(file);
                if (fileName is null)
                    continue;

                var charMatch = CharFileRegex().Match(fileName);
                if (charMatch.Success)
                {
                    AddEntry(SettingsFileKind.Char, charMatch.Groups[1].Value, fileName, file, serverFolderName,
                        profileName, result);
                    continue;
                }

                var userMatch = UserFileRegex().Match(fileName);
                if (userMatch.Success)
                {
                    AddEntry(SettingsFileKind.User, userMatch.Groups[1].Value, fileName, file, serverFolderName,
                        profileName, result);
                }
            }
        }
    }

    private static void AddEntry(
        SettingsFileKind kind,
        string id,
        string fileName,
        string fullPath,
        string serverFolderName,
        string profileFolderName,
        ScanResult result)
    {
        DateTime modifiedUtc;
        try
        {
            modifiedUtc = File.GetLastWriteTimeUtc(fullPath);
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Could not read '{fullPath}': {ex.Message}");
            return;
        }

        result.Files.Add(new SettingsFileEntry(kind, id, fileName, fullPath, serverFolderName, profileFolderName,
            modifiedUtc));
    }
}
