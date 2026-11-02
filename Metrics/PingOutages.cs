using System.IO;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Metrics;

/// <summary>Ping hedefine ulaşılamayan dönemler; uygulama kapalıyken bağlantı durumu bilinmez.</summary>
internal sealed class PingOutages
{
    internal sealed record Outage(string Host, DateTimeOffset Started, DateTimeOffset? Recovered);
    private readonly object _sync = new();
    private readonly List<Outage> _events;
    private Outage? _current;
    private readonly string _path;

    public PingOutages(string? path = null)
    {
        _path = path ?? Path.Combine(AppPaths.DataDir, "ping-outages.json");
        try { _events = File.Exists(_path) ? JsonSerializer.Deserialize<List<Outage>>(File.ReadAllText(_path)) ?? new() : new(); }
        catch (Exception ex)
        {
            Log.Warn("Ping geçmişi okunamadı.", ex);
            try { if (File.Exists(_path)) File.Copy(_path, _path + ".bad", overwrite: true); }
            catch (Exception copyEx) { Log.Warn("Ping geçmişi yedeklenemedi.", copyEx); }
            _events = new();
        }
    }

    public void Observe(string host, bool success, DateTimeOffset now)
    {
        lock (_sync)
        {
            if (_current is not null && !string.Equals(_current.Host, host, StringComparison.OrdinalIgnoreCase)) _current = null;
            if (!success && _current is null)
            {
                _current = new Outage(host, now, null);
                _events.Add(_current);
                if (_events.Count > 200) _events.RemoveRange(0, _events.Count - 200);
                Save();
            }
            else if (success && _current is not null)
            {
                int index = _events.IndexOf(_current);
                if (index >= 0) _events[index] = _current with { Recovered = now };
                _current = null;
                Save();
            }
        }
    }

    public void StopTracking() { lock (_sync) _current = null; }
    public List<Outage> Snapshot() { lock (_sync) return _events.AsEnumerable().Reverse().ToList(); }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(_events));
            File.Move(temp, _path, overwrite: true);
        }
        catch (Exception ex) { Log.Warn("Ping geçmişi kaydedilemedi.", ex); }
    }
}
