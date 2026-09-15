using System.Windows.Media;

namespace VibecoreHub.Desktop;

public static class ThemeService
{
    private static readonly string[] Themes = ["midnight", "porcelain", "aurora", "blush"];

    public static string Next(string current)
    {
        var index = Array.IndexOf(Themes, current);
        return Themes[(index + 1 + Themes.Length) % Themes.Length];
    }

    public static void Apply(string theme)
    {
        var colors = theme switch
        {
            "porcelain" => new[] { "#D6D6D2", "#E1E0DC", "#DAD9D5", "#E9E8E4", "#272923", "#6F746B", "#969B92", "#BCBEB8", "#3C4338", "#F3F4F0" },
            "aurora" => new[] { "#091018", "#0E1721", "#111D29", "#182736", "#F2FBFF", "#88A3B5", "#536F81", "#243847", "#65F3CF", "#06231C" },
            "blush" => new[] { "#E9E5E5", "#F1ECEC", "#ECE7E7", "#F8F3F3", "#3E373A", "#807479", "#A99DA1", "#D2C7CA", "#D77F99", "#FFFFFF" },
            _ => new[] { "#181818", "#242424", "#1E1E1E", "#303030", "#E7E7E7", "#9A9A9A", "#686868", "#393939", "#07C160", "#071B11" }
        };
        var keys = new[] { "WindowBrush", "BarBrush", "PanelBrush", "RaisedBrush", "TextBrush", "MutedBrush", "FaintBrush", "LineBrush", "AccentBrush", "AccentTextBrush" };
        for (var i = 0; i < keys.Length; i++) System.Windows.Application.Current.Resources[keys[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));
    }
}
