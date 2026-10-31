using System.Reflection;

namespace PulseMonitor.Core;

/// <summary>Sürüm adı (Lodos…): PulseMonitor.csproj → ReleaseName, derlemeye "ReleaseName" üstverisi olarak yazılır.</summary>
internal static class ReleaseInfo
{
    public static string Name { get; } = typeof(ReleaseInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "ReleaseName")?.Value?.Trim() ?? "";

    /// <summary>"0.15.2 · Lodos" ya da ad yoksa yalnızca sürüm.</summary>
    public static string WithName(string version) => Name.Length == 0 ? version : $"{version} · {Name}";
}
