namespace PulseMonitor.ViewModels;

/// <summary>
/// Ağ mini grafiği için son 24 saniyelik hız. Ölçek o penceredeki en yüksek değere göre kendiliğinden ayarlanır;
/// boşta küçük trafik grafiği doldurmasın diye en az 64 KB/s kabul edilir.
/// </summary>
internal sealed class SpeedHistory
{
    private const int Length = 24;
    private const double MinScaleBps = 64 * 1024;
    private readonly Queue<double> _values = new(Enumerable.Repeat(0.0, Length));

    /// <returns>0–1 arası yeni dizi (grafiğin yeniden çizilmesi için her seferinde yeni).</returns>
    public double[] Add(double bytesPerSecond)
    {
        _values.Enqueue(Math.Max(0, bytesPerSecond));
        if (_values.Count > Length) _values.Dequeue();
        double scale = Math.Max(MinScaleBps, _values.Max());
        return _values.Select(v => v / scale).ToArray();
    }
}
