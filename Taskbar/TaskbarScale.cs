namespace PulseMonitor.Taskbar;

/// <summary>Başlat düğmesinin solunda kalan fiziksel alana göre güvenli görev çubuğu ölçeği.</summary>
internal static class TaskbarScale
{
    // Ayarlardaki en küçük değer %70'tir. Çakışmayı önlemek için yalnızca geçici olarak
    // %55'e kadar inilir; bunun altı okunabilir bir görev çubuğu paneli değildir.
    public const double Minimum = 0.55;

    /// <summary>
    /// İstenen ölçek, doğal genişlik ve kullanılabilir fiziksel pikselden efektif ölçeği döndürür.
    /// Alan %55'in altını gerektiriyorsa null: panel görünmeyerek Başlat düğmesini korumalıdır.
    /// </summary>
    public static double? Fit(double requested, double naturalWidthDip, int availablePhysicalWidth, double dpiScale)
    {
        if (requested <= 0 || naturalWidthDip <= 0 || availablePhysicalWidth <= 0 || dpiScale <= 0) return null;

        double limit = availablePhysicalWidth / (naturalWidthDip * dpiScale);
        if (limit < Minimum) return null;
        return Math.Min(requested, limit);
    }
}
