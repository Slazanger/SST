using EveSettings.Core.Models;

namespace EveSettings.ViewModels;

public static class FileDisplayFormatter
{
    public static string Format(SettingsFileEntry entry, string? resolvedName)
    {
        var kind = entry.Kind == SettingsFileKind.Char ? "core_char" : "core_user";
        var fallback = entry.Kind == SettingsFileKind.Char ? "Character" : "Account";
        var label = string.IsNullOrWhiteSpace(resolvedName) ? fallback : resolvedName;
        return $"{label} ({entry.Id}) · {kind} · {entry.ProfileFolderName}";
    }
}
