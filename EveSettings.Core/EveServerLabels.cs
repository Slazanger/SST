namespace EveSettings.Core;

/// <summary>
/// Maps CCP local settings folder names (under %LOCALAPPDATA%\CCP\EVE) to short UI labels.
/// </summary>
public static class EveServerLabels
{
    public static string GetDisplayLabel(string serverFolderName)
    {
        if (string.IsNullOrWhiteSpace(serverFolderName))
            return "Unknown shard";

        var n = serverFolderName.Trim();

        if (ContainsIgnoreCase(n, "tranquility") || n.Equals("_tq_tranquility", StringComparison.OrdinalIgnoreCase))
            return "Tranquility (TQ)";

        if (ContainsIgnoreCase(n, "singularity"))
            return "Singularity (Sisi)";

        if (ContainsIgnoreCase(n, "serenity"))
            return "Serenity";

        if (ContainsIgnoreCase(n, "infinity"))
            return "Infinity";

        if (ContainsIgnoreCase(n, "duality"))
            return "Duality";

        if (ContainsIgnoreCase(n, "thunderdome"))
            return "Thunderdome";

        return $"Unknown shard ({n})";
    }

    private static bool ContainsIgnoreCase(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
