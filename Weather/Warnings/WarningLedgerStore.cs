using System.IO;
using System.Text.Json;
using PulseMonitor.Core;

namespace PulseMonitor.Weather.Warnings;

/// <summary>Bildirim kaydını <c>weather-warnings.json</c>'a yazar/okur (atomik; bozuksa yok sayılır, program çökmez).</summary>
internal static class WarningLedgerStore
{
    public static WarningLedger Load()
    {
        try
        {
            if (!File.Exists(AppPaths.WeatherWarningsFile)) return new WarningLedger();
            return JsonSerializer.Deserialize<WarningLedger>(File.ReadAllText(AppPaths.WeatherWarningsFile)) ?? new WarningLedger();
        }
        catch (Exception ex)
        {
            Log.Warn("Hava uyarı kaydı okunamadı; yok sayıldı.", ex);
            return new WarningLedger();
        }
    }

    public static void Save(WarningLedger ledger)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            string temp = AppPaths.WeatherWarningsFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(ledger));
            File.Move(temp, AppPaths.WeatherWarningsFile, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Warn("Hava uyarı kaydı yazılamadı.", ex);
        }
    }
}
