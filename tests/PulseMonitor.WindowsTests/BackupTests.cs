using System.IO;
using PulseMonitor.Settings;
using PulseMonitor.Traffic;

namespace PulseMonitor.WindowsTests;

public sealed class BackupTests
{
    [Fact]
    public void SettingsRoundTripPreservesKeyValues()
    {
        string path = Path.Combine(Path.GetTempPath(), $"pm-{Guid.NewGuid():N}.json");
        try
        {
            BackupTransfer.ExportSettings(path, new AppSettings { Language = "en", TaskbarLeft = true, TrayGapPx = 27 });
            var loaded = BackupTransfer.ReadSettings(path);
            Assert.Equal("en", loaded.Language);
            Assert.True(loaded.TaskbarLeft);
            Assert.Equal(27, loaded.TrayGapPx);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void HistoryRoundTripAndWrongTypeRejected()
    {
        string path = Path.Combine(Path.GetTempPath(), $"pm-{Guid.NewGuid():N}.json");
        try
        {
            var history = new TrafficHistory();
            var day = new DateOnly(2026, 1, 2);
            history.Add(day, 123, 456);
            BackupTransfer.ExportHistory(path, history);
            var item = Assert.Single(BackupTransfer.ReadHistory(path));
            Assert.Equal(day, item.Key);
            Assert.Equal(new DayTraffic(123, 456), item.Value);
            Assert.Throws<InvalidDataException>(() => BackupTransfer.ReadSettings(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void InvalidHistoryIsRejectedBeforeImport()
    {
        string path = Path.Combine(Path.GetTempPath(), $"pm-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{\"Version\":1,\"Kind\":\"traffic\",\"Days\":{\"not-a-date\":[1,2]}}");
            Assert.Throws<InvalidDataException>(() => BackupTransfer.ReadHistory(path));
        }
        finally { File.Delete(path); }
    }
}
