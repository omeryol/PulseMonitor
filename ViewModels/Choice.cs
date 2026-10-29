using PulseMonitor.Core;
using PulseMonitor.Settings;

namespace PulseMonitor.ViewModels;

/// <summary>Açılır listede bir seçenek: kimlik + ekrandaki ad.</summary>
public sealed record Option(string Id, string Display);

/// <summary>
/// Bir görünüm seçeneği grubu (ör. "Arrows"): seçenekler StyleOptions.Groups'tan, adlar
/// lang/tr.json → "Style.&lt;grup&gt;.&lt;kimlik&gt;"dan gelir. Seçim değişince ayar yazılır.
/// </summary>
public sealed class Choice : ObservableObject
{
    private readonly Func<string> _get;
    private readonly Action<string> _set;

    public Choice(string group, Func<string> get, Action<string> set)
    {
        _get = get;
        _set = set;
        Options = StyleOptions.Groups[group].Select(id => new Option(id, Strings.Get($"Style.{group}.{id}"))).ToList();
    }

    public IReadOnlyList<Option> Options { get; }

    public Option? Selected
    {
        get => Options.FirstOrDefault(o => o.Id == _get());
        set
        {
            if (value is null || value.Id == _get()) return;
            _set(value.Id);
            Raise(nameof(Selected));
        }
    }

    public void Refresh() => Raise(nameof(Selected));
}
