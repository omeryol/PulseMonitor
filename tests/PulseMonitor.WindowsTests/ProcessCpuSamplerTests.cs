using PulseMonitor.Metrics;

namespace PulseMonitor.WindowsTests;

public sealed class ProcessCpuSamplerTests
{
    private static ProcessSample S(int id, string name, double cpuSeconds = 0, ulong ram = 0) =>
        new(id, name, TimeSpan.FromSeconds(cpuSeconds), ram);

    [Fact]
    public void PercentUsesAllLogicalProcessorsAndRejectsInvalidIntervals()
    {
        Assert.Equal(25, ProcessUsage.CalculatePercent(TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 4));
        Assert.Equal(0, ProcessUsage.CalculatePercent(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 4));
        Assert.Equal(0, ProcessUsage.CalculatePercent(TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.Zero, 4));
        Assert.Equal(0, ProcessUsage.CalculatePercent(TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 0));
    }

    [Fact]
    public void CpuGroupsSameNameAndOrdersByUsageThenName()
    {
        var previous = new[] { S(1, "chrome"), S(2, "chrome"), S(3, "Chrome"), S(4, "zeta"), S(5, "alpha"), S(6, "bravo"),
            S(7, "delta"), S(8, "echo") }.ToDictionary(s => s.Id);
        // 1 sn, 1 çekirdek: CPU saniyesi doğrudan yüzde/100.
        var current = new[] { S(1, "chrome", 0.1), S(2, "chrome", 0.1), S(3, "Chrome", 0.1), S(4, "zeta", 0.05),
            S(5, "alpha", 0.2), S(6, "bravo", 0.2), S(7, "delta", 0.08), S(8, "echo", 0.06) };

        var top = ProcessUsage.TopCpu(previous, current, TimeSpan.FromSeconds(1), 1);

        Assert.Equal(["chrome", "alpha", "bravo", "delta", "echo"], top.Select(r => r.Name));
        Assert.Equal(3, top[0].Count);
        Assert.Equal(30, top[0].Percent, 6);
        Assert.Equal(1, top[1].Count);
    }

    [Fact]
    public void CpuSkipsNewAndReusedProcessIdsAndCapsGroupAt100()
    {
        var previous = new[] { S(1, "busy"), S(2, "busy"), S(3, "old") }.ToDictionary(s => s.Id);
        var current = new[] { S(1, "busy", 0.8), S(2, "busy", 0.8), S(3, "new-owner", 0.5), S(9, "fresh", 0.9) };

        var top = ProcessUsage.TopCpu(previous, current, TimeSpan.FromSeconds(1), 1);

        Assert.Equal(["busy"], top.Select(r => r.Name));
        Assert.Equal(100, top[0].Percent);
    }

    [Fact]
    public void MemoryGroupsSameNameAndSumsWorkingSet()
    {
        var samples = new[] { S(1, "chrome", ram: 4), S(2, "chrome", ram: 7), S(3, "alpha", ram: 10), S(4, "bravo", ram: 10),
            S(5, "charlie", ram: 8), S(6, "delta", ram: 6), S(7, "echo", ram: 5) };

        var top = ProcessUsage.TopMemory(samples);

        Assert.Equal(["chrome", "alpha", "bravo", "charlie", "delta"], top.Select(r => r.Name));
        Assert.Equal(11UL, top[0].WorkingSet);
        Assert.Equal(2, top[0].Count);
    }

    [Fact]
    public async Task NothingIsPublishedAfterTheCardCloses()
    {
        using var sampler = new ProcessCpuSampler();
        var events = new List<ProcessDetailsSnapshot>();
        sampler.Updated += snapshot => { lock (events) events.Add(snapshot); };

        // Hızlı aç-kapa: ilk ölçüm sürerken kart kapanır, sonra yeniden açılıp kapanır.
        sampler.SetActive(true);
        sampler.SetActive(false);
        sampler.SetActive(true);
        await Task.Delay(1700);
        sampler.SetActive(false);
        int countAtClose;
        lock (events) countAtClose = events.Count;
        await Task.Delay(1500);

        lock (events)
        {
            Assert.Equal(countAtClose, events.Count);
            Assert.Same(ProcessDetailsSnapshot.Empty, events[^1]);
            Assert.Contains(events, e => !e.CpuLoading && e.Cpu.Count > 0);
            Assert.Contains(events, e => e.Memory.Count > 0);
        }
    }

    [Fact]
    public void EmptyInputGivesEmptyLists()
    {
        Assert.Empty(ProcessUsage.TopMemory([]));
        Assert.Empty(ProcessUsage.TopCpu(new Dictionary<int, ProcessSample>(), [], TimeSpan.FromSeconds(1), 4));
    }
}
