using System.Windows;

namespace PulseMonitor.ViewModels;

/// <summary>
/// "Hıza göre" değer kalınlığı için ortak ölçek: son dakikalardaki en yüksek hız (kayan pencere; tek ani tepe sonsuza dek
/// ölçeği bozmaz). Tepe en az 1 MB/s sayılır, yavaş hatlarda her değer en kalın görünmesin.
/// </summary>
internal sealed class SpeedWeightScale
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private const double Floor = 1024 * 1024;

    private readonly LinkedList<(DateTime Time, double Value)> _maxima = new(); // değerler azalan sırada

    public double Peak { get; private set; } = Floor;

    public void Add(double bytesPerSecond)
    {
        DateTime now = DateTime.UtcNow;
        while (_maxima.Last is { } last && last.Value.Value <= bytesPerSecond) _maxima.RemoveLast();
        _maxima.AddLast((now, bytesPerSecond));
        while (_maxima.First!.Value.Time < now - Window) _maxima.RemoveFirst();
        Peak = Math.Max(Floor, _maxima.First.Value.Value);
    }
}

/// <summary>
/// Bir satırın kalınlığı ve boyutu, gözle ayrılan üç basamak: Normal (&lt; 100 KB/s), Kalın, En kalın ve iri (tepenin %60’ı ve
/// üstü, en az 1 MB/s). Yazı küçük olduğundan ince (Light) kullanılmaz; yarı kalın ile kalın ayırt edilemediği için atlanır.
/// Eşikte gidip gelmesin diye geçişlerde %10 pay vardır. Yer tutucu en büyük basamağa (<see cref="MaxSizeBonus"/>) göre ölçülür.
/// </summary>
internal sealed class SpeedWeight(SpeedWeightScale scale)
{
    private static readonly FontWeight[] Steps = [FontWeights.Normal, FontWeights.Bold, FontWeights.Black];
    private const double BoldFrom = 100 * 1024, BlackShare = 0.6, Margin = 0.1;
    private static readonly double[] SizeBonuses = [0, 1, 2];
    private int _step;

    public static double MaxSizeBonus => SizeBonuses[^1];
    public static FontWeight WeightOf(int step) => Steps[step];

    /// <summary>Punto cinsinden ek boyut (temel yazı boyutuna eklenir).</summary>
    public static double SizeBonusOf(int step) => SizeBonuses[step];

    /// <summary>Basamak: 0 normal, 1 kalın, 2 en kalın ve iri.</summary>
    public int Next(double bytesPerSecond)
    {
        double[] from = [0, BoldFrom, Math.Max(1024 * 1024, scale.Peak * BlackShare)];
        while (_step < Steps.Length - 1 && bytesPerSecond >= from[_step + 1] * (1 + Margin)) _step++;
        while (_step > 0 && bytesPerSecond < from[_step] * (1 - Margin)) _step--;
        return _step;
    }
}
