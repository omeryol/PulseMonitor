using System.IO;
using PulseMonitor.Core;

namespace PulseMonitor.Metrics;

/// <summary>Bir sürücünün boş ve toplam alanı (bayt). <see cref="Drive"/> "C:" biçimindedir.</summary>
public sealed record DiskSpace(string Drive, long Free, long Total)
{
    /// <summary>Dolu oran (0–100); çubuk için.</summary>
    public double UsedPercent => Total <= 0 ? 0 : Math.Clamp((Total - Free) * 100.0 / Total, 0, 100);
}

/// <summary>
/// Takılı sabit diskler ve boş alanları. Yalnızca Disk ayrıntı kartı açıkken, saniyede bir okunur.
/// Hazır olmayan sürücü (kilitli, çıkarılmış) hata değildir ve listeden çıkar; beklenmedik hata günlüğe bir kez yazılır.
/// </summary>
internal sealed class DiskSpaceReader
{
    private bool _failed;

    public IReadOnlyList<DiskSpace> ReadAll()
    {
        var drives = new List<DiskSpace>();
        try
        {
            foreach (DriveInfo info in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
                if (Read(info) is { } space) drives.Add(space);
            _failed = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!_failed) Log.Warn("Sürücü listesi okunamadı.", ex);
            _failed = true;
        }
        return drives;
    }

    internal static DiskSpace? Read(DriveInfo info)
    {
        try
        {
            return info.IsReady ? new DiskSpace(info.Name.TrimEnd('\\'), info.AvailableFreeSpace, info.TotalSize) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
