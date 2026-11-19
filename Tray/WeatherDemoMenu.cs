#if DEBUG
using System.Windows.Controls;
using PulseMonitor.Core;
using PulseMonitor.Weather;

namespace PulseMonitor.Tray;

internal sealed record WeatherDemo(int Code, double Temperature, string? Variant = null, bool Day = true, double Wind = 0)
{
    public string Label => Variant is null ? WeatherCodes.Describe(Code)
        : $"{WeatherCodes.Describe(Code)} · {Strings.Get($"Demo.{Variant}")}";

    public WeatherReport Report() => new()
    {
        FetchedAtUtc = DateTime.UtcNow, WeatherCode = Code, IsDay = Day,
        WindKmh = Wind, Temperature = Temperature, ApparentTemperature = Temperature,
    };
}

internal static class WeatherDemoMenu
{
    internal static readonly WeatherDemo First = new(97, 17, "Heavy", Wind: 48);

    /// <summary>Komut satırından örnek hava: "kod,sıcaklık,rüzgâr km/sa,gündüz (1/0)"; ekran görüntüsü/GIF üretimi için.</summary>
    public static WeatherDemo? Parse(string text)
    {
        string[] p = text.Split(',');
        var c = System.Globalization.CultureInfo.InvariantCulture;
        if (p.Length < 2 || !int.TryParse(p[0], c, out int code) || !double.TryParse(p[1], c, out double temp)) return null;
        double wind = p.Length > 2 && double.TryParse(p[2], c, out double w) ? w : 0;
        bool day = p.Length < 4 || p[3] != "0";
        return new WeatherDemo(code, temp, Wind: wind, Day: day);
    }

    public static MenuItem Create(Action<WeatherDemo?> select, WeatherDemo? initial)
    {
        var root = new MenuItem { Header = Strings.Get("Demo.Menu") };
        var choices = new List<(MenuItem Item, WeatherDemo? Demo)>();
        void Add(ItemCollection items, WeatherDemo? demo)
        {
            var item = new MenuItem { Header = demo?.Label ?? Strings.Get("Demo.Live"), IsChecked = demo == initial };
            item.Click += (_, _) =>
            {
                foreach (var choice in choices) choice.Item.IsChecked = choice.Demo == demo;
                select(demo);
            };
            choices.Add((item, demo));
            items.Add(item);
        }
        void Group(string label, params WeatherDemo[] demos)
        {
            var group = new MenuItem { Header = Strings.Get(label) };
            foreach (var demo in demos) Add(group.Items, demo);
            root.Items.Add(group);
        }
        Add(root.Items, null);
        root.Items.Add(new Separator());
        Group("Demo.Sky", new(0, 23, "Day"), new(0, 9, "Night", false), new(1, 21), new(2, 17), new(2, 16, "Wind", Wind: 38), new(3, 12), new(45, 6), new(48, -2, "Rime"));
        Group("Weather.Code.Rain", new(61, 13, "Light"), new(63, 12), new(65, 10, "Heavy"),
            new(80, 14, "Light"), new(81, 12), new(82, 10, "Heavy"));
        Group("Weather.Code.Drizzle", new(51, 13, "Light"), new(53, 12), new(55, 11, "Heavy"));
        Group("Weather.Code.FreezingRain", new(56, -1, "Fine"), new(57, -2, "DenseFine"),
            new(66, -1, "Light"), new(67, -3, "Heavy"));
        Group("Weather.Code.Snow", new(71, 0, "Light"), new(73, -1), new(75, -3, "Heavy"),
            new(77, -4, "Grains"), new(85, -1), new(86, -3, "Heavy"));
        Group("Weather.Code.Thunderstorm", new(95, 18), First, new(96, 9), new(99, 7, "Heavy"));
        return root;
    }
}
#endif
