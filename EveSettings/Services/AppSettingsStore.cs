using System.Text.Json;

namespace EveSettings.Services;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public AppSettingsStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EveSettings");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "appSettings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(_path))
                return new AppSettings();

            var json = await File.ReadAllTextAsync(_path, ct);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_path, json, ct);
    }
}

public sealed class AppSettings
{
    public string? LastEveRootPath { get; set; }

    /// <summary>Last selected CCP server folder name under the EVE root (e.g. <c>c_tranquility</c>).</summary>
    public string? LastServerFolderName { get; set; }
}
