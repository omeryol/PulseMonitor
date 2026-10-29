using System.Net;
using System.Net.Http;
using System.Text.Json;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Platform;

public enum UpdateState { Unknown, UpToDate, Available, Failed }

/// <summary>
/// GitHub'daki son sürümü denetler (açılıştan 1 dk sonra, sonra günde bir; ayardan kapatılabilir).
/// Sürüm etiketi, sürüm notları ve kurulum dosyasının bağlantısı okunur; hiçbir veri gönderilmez. <see cref="Changed"/> arka planda çağrılır.
/// </summary>
internal sealed class UpdateChecker : IDisposable
{
    public const string ReleasesPage = "https://github.com/omeryol/PulseMonitor/releases/latest";
    private const string Api = "https://api.github.com/repos/omeryol/PulseMonitor/releases/latest";
    private static readonly TimeSpan FirstCheck = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    private readonly AppSettings _settings;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public UpdateState State { get; private set; }
    public string? LatestVersion { get; private set; }
    public string ReleaseUrl { get; private set; } = ReleasesPage;
    /// <summary>Kurulum dosyasının doğrudan indirme bağlantısı; bulunamazsa sürüm sayfası.</summary>
    public string DownloadUrl { get; private set; } = ReleasesPage;
    /// <summary>Sürüm notları (GitHub sürüm metni, Markdown; dillere göre bölümlü: <see cref="ReleaseNotes"/>).</summary>
    public string Notes { get; private set; } = "";
    public DateTime? LastChecked { get; private set; }
    public static Version Current { get; } = typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(0, 0, 0);

    public event Action? Changed;

    public UpdateChecker(AppSettings settings)
    {
        _settings = settings;
        _timer = new Timer(_ => { if (_settings.CheckForUpdates) _ = CheckAsync(); });
    }

    public void Start() => _timer.Change(FirstCheck, Interval);

    public async Task CheckAsync()
    {
        if (!await _gate.WaitAsync(0)) return;
        try
        {
            using var response = await Http.Client.GetAsync(Api);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                State = UpdateState.UpToDate; // henüz yayınlanmış sürüm yok
                return;
            }
            response.EnsureSuccessStatusCode();
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
            string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            if (doc.RootElement.TryGetProperty("html_url", out JsonElement url) && url.GetString() is { } u) ReleaseUrl = u;
            Notes = doc.RootElement.TryGetProperty("body", out JsonElement body) ? body.GetString() ?? "" : "";
            DownloadUrl = SetupAssetUrl(doc.RootElement) ?? ReleaseUrl;

            if (Version.TryParse(tag.TrimStart('v', 'V'), out Version? latest) && Normalize(latest) > Normalize(Current))
            {
                LatestVersion = latest.ToString(3);
                State = UpdateState.Available;
            }
            else
            {
                State = UpdateState.UpToDate;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            if (State != UpdateState.Failed) Log.Warn("Güncelleme denetlenemedi.", ex);
            State = UpdateState.Failed;
        }
        finally
        {
            LastChecked = DateTime.Now;
            _gate.Release();
            Changed?.Invoke();
        }
    }

    /// <summary>Bu sürüm için daha önce bildirim gösterilmediyse true döner ve sürümü kaydeder (aynı sürüm için bir kez).</summary>
    public bool ShouldNotify(SettingsStore store)
    {
        if (State != UpdateState.Available || LatestVersion is null || store.Current.LastNotifiedVersion == LatestVersion) return false;
        store.Current.LastNotifiedVersion = LatestVersion;
        store.Save();
        return true;
    }

    /// <summary>Sürüm dosyaları arasında "PulseMonitor-Setup-x.y.z.exe".</summary>
    private static string? SetupAssetUrl(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array) return null;
        foreach (JsonElement a in assets.EnumerateArray())
        {
            string name = a.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? "" : "";
            if (name.StartsWith("PulseMonitor-Setup-", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                && a.TryGetProperty("browser_download_url", out JsonElement d))
                return d.GetString();
        }
        return null;
    }

    private static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build));

    public void Dispose() => _timer.Dispose();
}
