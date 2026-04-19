namespace SST.Core;

public static class EvePaths
{
    /// <summary>
    /// Default Windows location for local EVE settings (per CCP layout).
    /// </summary>
    public static string GetDefaultWindowsCcpEveRoot()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "CCP", "EVE");
    }

    public static bool DirectoryExists(string path) =>
        Directory.Exists(path);
}
