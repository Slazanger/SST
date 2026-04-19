namespace EveSettings.Core.Models;

public sealed class ScanResult
{
    public List<SettingsFileEntry> Files { get; } = [];
    public List<string> Warnings { get; } = [];
}
