namespace EveSettings.Core.Models;

/// <summary>
/// A discovered <c>core_char_*.dat</c> or <c>core_user_*.dat</c> file under an EVE local settings tree.
/// </summary>
public sealed record SettingsFileEntry(
    SettingsFileKind Kind,
    string Id,
    string FileName,
    string FullPath,
    string ServerFolderName,
    string ProfileFolderName,
    DateTime ModifiedUtc);
