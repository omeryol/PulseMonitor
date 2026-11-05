using System.Net.NetworkInformation;
using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.Metrics;

/// <summary>
/// Seçili sunucuya 5 saniyede bir ping (ICMP; yönetici yetkisi gerekmez). Yalnızca "Ping" öğesi açıkken çalışır.
/// <see cref="Changed"/> arka planda çağrılır; yanıt yoksa <see cref="LatencyMs"/> null.
/// </summary>
internal sealed class PingMonitor : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private const int TimeoutMs = 2000;

    private readonly AppSettings _settings;
    private readonly Timer _timer;
    internal PingOutages Outages { get; } = new();
    private int _busy;
    private string? _lastError;

    public double? LatencyMs { get; private set; }

    private const int HistorySize = 12; // son 1 dakika (5 sn aralıkla)
    private readonly Queue<double?> _history = new();

    /// <summary>Son 1 dakikanın gecikme, jitter ve kayıp özeti; ayrıntı kartı için.</summary>
    public PingQuality Stats
    {
        get
        {
            lock (_history)
            {
                return PingQuality.FromSamples(_history);
            }
        }
    }
    public event Action? Changed;

    public PingMonitor(AppSettings settings)
    {
        _settings = settings;
        _timer = new Timer(_ => _ = PingAsync());
    }

    public void Start() => _timer.Change(TimeSpan.Zero, Interval);

    private async Task PingAsync()
    {
        if (!_settings.IsShown(TaskbarItemId.Ping)) { Outages.StopTracking(); return; }
        if (Interlocked.Exchange(ref _busy, 1) == 1) return;
        string host = _settings.PingHost;
        try
        {
            using var ping = new Ping();
            PingReply reply = await ping.SendPingAsync(host, TimeoutMs);
            LatencyMs = reply.Status == IPStatus.Success ? reply.RoundtripTime : null;
            _lastError = null;
        }
        catch (Exception ex) when (ex is PingException or ArgumentException or InvalidOperationException)
        {
            LatencyMs = null;
            if (ex.Message != _lastError) Log.Warn($"Ping başarısız: {_settings.PingHost}", ex);
            _lastError = ex.Message;
        }
        finally
        {
            if (_settings.IsShown(TaskbarItemId.Ping) && string.Equals(host, _settings.PingHost, StringComparison.OrdinalIgnoreCase))
                Outages.Observe(host, LatencyMs.HasValue, DateTimeOffset.Now);
            else Outages.StopTracking();
            lock (_history)
            {
                _history.Enqueue(LatencyMs);
                while (_history.Count > HistorySize) _history.Dequeue();
            }
            Interlocked.Exchange(ref _busy, 0);
            Changed?.Invoke();
        }
    }

    public void Dispose() => _timer.Dispose();
}

public readonly record struct PingQuality(double? Min, double? Average, double? Max, double? Jitter, double LossPercent, int SampleCount)
{
    public static PingQuality FromSamples(IEnumerable<double?> samples)
    {
        var all = samples.ToList();
        if (all.Count == 0) return new(null, null, null, null, 0, 0);
        var ok = all.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        double loss = 100.0 * (all.Count - ok.Count) / all.Count;
        double? jitter = ok.Count < 2 ? null : ok.Zip(ok.Skip(1), (a, b) => Math.Abs(b - a)).Average();
        return ok.Count == 0 ? new(null, null, null, null, loss, all.Count) : new(ok.Min(), ok.Average(), ok.Max(), jitter, loss, all.Count);
    }
}
