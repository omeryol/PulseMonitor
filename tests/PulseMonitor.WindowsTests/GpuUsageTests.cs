using PulseMonitor.Metrics;

namespace PulseMonitor.WindowsTests;

public sealed class GpuUsageTests
{
    private const string A = "luid_0x00000000_0x0000D1A3_phys_0";
    private const string B = "luid_0x00000000_0x0000E2B4_phys_0";
    private static string Engine(int pid, string luid, int eng, string type) => $"pid_{pid}_{luid}_eng_{eng}_engtype_{type}";

    [Fact]
    public void InstanceNamesAreParsed()
    {
        string name = Engine(1234, A, 1, "VideoDecode");
        Assert.Equal("luid_0x0_0xD1A3", GpuInstanceName.Luid(name));
        Assert.Equal(1234, GpuInstanceName.Pid(name));
        Assert.Equal("VideoDecode", GpuInstanceName.EngineType(name));
        Assert.Equal("phys_0_eng_1", GpuInstanceName.EngineId(name));

        Assert.Equal("luid_0x0_0xD1A3", GpuInstanceName.Luid(A)); // adaptör belleği örneği
        Assert.Null(GpuInstanceName.Pid(A));
        Assert.Null(GpuInstanceName.EngineType(A));
        Assert.Null(GpuInstanceName.Luid("_Total"));
        Assert.Null(GpuInstanceName.Luid("luid_0xZZ_0x1_phys_0"));
    }

    [Theory]
    [InlineData("3D", "3D")]
    [InlineData("Compute_0", "Compute")]
    [InlineData("Cuda", "Compute")]
    [InlineData("VideoProcessing", "VideoProcessing")]
    [InlineData("Security", null)]
    [InlineData(null, null)]
    public void EngineTypesAreNormalized(string? type, string? expected) => Assert.Equal(expected, GpuUsage.NormalizeEngine(type));

    [Fact]
    public void EngineLoadSumsProcessesOnTheSameEngineAndTakesTheBusiestEngine()
    {
        var engines = new (string, double)[]
        {
            (Engine(1, A, 0, "3D"), 30), (Engine(2, A, 0, "3D"), 20), (Engine(3, A, 1, "3D"), 10), // eng_0 = 50, eng_1 = 10
            (Engine(2, A, 0, "VideoDecode"), 5),
            (Engine(9, B, 0, "3D"), 90), // başka adaptör
        };

        GpuDetails all = GpuUsage.Summarize(engines, [], [], new HashSet<string>(), 0, _ => "x", 5);
        Assert.Equal(90, all.Engines.Single(e => e.Type == "3D").Percent);

        GpuDetails onlyA = GpuUsage.Summarize(engines, [], [], new HashSet<string> { "luid_0x0_0xD1A3" }, 0, _ => "x", 5);
        Assert.Equal(50, onlyA.Engines.Single(e => e.Type == "3D").Percent);
        Assert.Equal(5, onlyA.Engines.Single(e => e.Type == "VideoDecode").Percent);
        Assert.Equal(0, onlyA.Engines.Single(e => e.Type == "Copy").Percent);
        Assert.Equal(GpuUsage.EngineOrder, onlyA.Engines.Select(e => e.Type));
    }

    [Fact]
    public void ProcessesAreGroupedByNameRankedAndTrimmed()
    {
        var engines = new (string, double)[]
        {
            (Engine(1, A, 0, "3D"), 30), (Engine(2, A, 0, "3D"), 20), (Engine(2, A, 0, "VideoDecode"), 5),
            (Engine(3, A, 0, "3D"), 25), (Engine(4, A, 0, "3D"), 0.01), (Engine(5, A, 0, "3D"), 12),
        };
        string? Name(int pid) => pid switch { 1 => "game", 2 => "chrome", 3 => "chrome", 5 => null, _ => "idle" };

        GpuDetails details = GpuUsage.Summarize(engines, [], [], new HashSet<string>(), 0, Name, 2);

        Assert.Equal(2, details.Processes.Count);
        Assert.Equal("chrome", details.Processes[0].Name); // 20 + 25
        Assert.Equal(45, details.Processes[0].Percent);
        Assert.Equal(2, details.Processes[0].Count);
        Assert.Equal("game", details.Processes[1].Name);
    }

    [Fact]
    public void MemoryIsSummedPerSelectedAdapter()
    {
        var dedicated = new (string, double)[] { (A, 2_000), (B, 500) };
        var shared = new (string, double)[] { (A, 100), (B, 50) };

        GpuDetails both = GpuUsage.Summarize([], dedicated, shared, new HashSet<string>(), 8_000, _ => null, 5);
        Assert.Equal(2_500UL, both.DedicatedUsed);
        Assert.Equal(150UL, both.SharedUsed);
        Assert.Equal(8_000UL, both.DedicatedTotal);

        GpuDetails one = GpuUsage.Summarize([], dedicated, shared, new HashSet<string> { "luid_0x0_0xE2B4" }, 4_000, _ => null, 5);
        Assert.Equal(500UL, one.DedicatedUsed);
        Assert.Equal(50UL, one.SharedUsed);
    }

    [Fact]
    public async Task SamplerReadsLiveCountersOnThisMachine()
    {
        using var sampler = new GpuDetailsSampler();
        sampler.Sample(null); // taban
        await Task.Delay(1200);
        GpuDetails details = sampler.Sample(null);

        Assert.Equal(GpuUsage.EngineOrder, details.Engines.Select(e => e.Type));
        Assert.All(details.Engines, e => Assert.InRange(e.Percent, 0, 100));
        Assert.All(details.Processes, p => Assert.InRange(p.Percent, 0, 100));
        Assert.True(details.Processes.Count <= 5);
    }
}
