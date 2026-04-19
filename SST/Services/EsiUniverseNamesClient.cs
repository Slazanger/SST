using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SST.Services;

/// <summary>
/// Resolves character IDs to names via public ESI <c>POST /universe/names/</c> (character category only).
/// </summary>
public sealed class EsiUniverseNamesClient
{
    public const int DefaultBatchSize = 1000;
    public const int MaxDiskCacheEntries = 8000;

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private static readonly HttpClient SharedHttp = CreateHttpClient();
    private readonly HttpClient _http;
    private readonly string _cachePath;
    private readonly Dictionary<long, string> _memory = new();
    private readonly object _diskLock = new();

    public EsiUniverseNamesClient(HttpClient? http = null)
    {
        _http = http ?? SharedHttp;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SST");
        Directory.CreateDirectory(dir);
        // Renamed from esi-names-cache.json so older caches that mixed user IDs are not reused.
        _cachePath = Path.Combine(dir, "esi-character-names-cache.json");
        LoadDiskIntoMemory();
    }

    private static HttpClient CreateHttpClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        // ESI expects a descriptive User-Agent (see EVE developer blog on ESI usage).
        c.DefaultRequestHeaders.UserAgent.ParseAdd(
            "SST/1.0 (Windows; +https://github.com/) eve-settings-helper");
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return c;
    }

    private void LoadDiskIntoMemory()
    {
        try
        {
            if (!File.Exists(_cachePath))
                return;

            var json = File.ReadAllText(_cachePath);
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonReadOptions);
            if (map is null)
                return;

            foreach (var (k, v) in map)
            {
                if (long.TryParse(k, out var id) && !string.IsNullOrWhiteSpace(v))
                    _memory[id] = v;
            }
        }
        catch
        {
            // ignore bad cache
        }
    }

    private void SaveDiskCache()
    {
        lock (_diskLock)
        {
            try
            {
                var pairs = _memory.OrderBy(p => p.Key).Take(MaxDiskCacheEntries)
                    .ToDictionary(p => p.Key.ToString(), p => p.Value);
                var json = JsonSerializer.Serialize(pairs,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_cachePath, json);
            }
            catch
            {
                // ignore IO errors
            }
        }
    }

    /// <summary>Stable ESI version path (avoids <c>/latest/</c> alias edge cases).</summary>
    public static string GetNamesEndpoint(EsiCluster cluster) =>
        cluster switch
        {
            EsiCluster.Serenity => "https://ali-esi.evepc.163.com/latest/universe/names/?datasource=serenity",
            EsiCluster.Infinity => "https://ali-esi.evepc.163.com/latest/universe/names/?datasource=infinity",
            _ => "https://esi.evetech.net/v1/universe/names/",
        };

    /// <summary>
    /// Returns character id → name for IDs ESI classified as <c>character</c>.
    /// </summary>
    public async Task<IReadOnlyDictionary<long, string>> ResolveCharacterNamesAsync(
        IReadOnlyList<long> ids,
        EsiCluster cluster,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return new Dictionary<long, string>();

        var result = new Dictionary<long, string>();
        foreach (var id in ids.Distinct())
        {
            if (_memory.TryGetValue(id, out var cached))
                result[id] = cached;
        }

        var missing = ids.Where(id => !result.ContainsKey(id)).Distinct().ToList();
        if (missing.Count == 0)
            return result;

        var url = GetNamesEndpoint(cluster);
        var batchSize = DefaultBatchSize;
        for (var i = 0; i < missing.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = missing.Skip(i).Take(batchSize).ToList();

            var payload = JsonSerializer.Serialize(chunk);
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"ESI universe/names failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {Truncate(body, 500)}");
            }

            List<EsiNameRow>? rows;
            try
            {
                rows = JsonSerializer.Deserialize<List<EsiNameRow>>(body, JsonReadOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"ESI universe/names JSON parse failed. Body: {Truncate(body, 500)}", ex);
            }

            if (rows is null)
                continue;

            foreach (var row in rows)
            {
                if (!string.Equals(row.Category, "character", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(row.Name))
                    continue;

                result[row.Id] = row.Name;
                _memory[row.Id] = row.Name;
            }
        }

        SaveDiskCache();
        return result;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private sealed record EsiNameRow(
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string? Name);
}
