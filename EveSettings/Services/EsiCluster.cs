namespace EveSettings.Services;

/// <summary>
/// Which ESI host to use for <c>/universe/names/</c> (public, unauthenticated).
/// </summary>
public enum EsiCluster
{
    Tranquility,
    Serenity,
    Infinity,
}

public static class EveFolderEsiRouting
{
    public static EsiCluster GetClusterForFolder(string serverFolderName)
    {
        if (string.IsNullOrWhiteSpace(serverFolderName))
            return EsiCluster.Tranquility;

        if (serverFolderName.Contains("serenity", StringComparison.OrdinalIgnoreCase))
            return EsiCluster.Serenity;

        if (serverFolderName.Contains("infinity", StringComparison.OrdinalIgnoreCase))
            return EsiCluster.Infinity;

        return EsiCluster.Tranquility;
    }
}
