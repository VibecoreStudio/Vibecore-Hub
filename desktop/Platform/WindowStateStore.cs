using System.IO;
using System.Text.Json;

namespace VibecoreHub.Desktop.Platform;

internal sealed class WidgetWindowState
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public List<string> OpenModuleIds { get; set; } = [];
    public Dictionary<string, double> ModuleHeights { get; set; } = [];
}

internal static class WindowStateStore
{
    private static string DataFile => Path.Combine(DataProfileManager.CurrentDirectory, "window-state.json");

    public static WidgetWindowState? Load()
    {
        try
        {
            var state = File.Exists(DataFile) ? JsonSerializer.Deserialize<WidgetWindowState>(File.ReadAllText(DataFile)) : null;
            if (state is not null) { state.OpenModuleIds ??= []; state.ModuleHeights ??= []; }
            return state;
        }
        catch { return null; }
    }

    public static void Save(WidgetWindowState state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
            var temporary = DataFile + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(state));
            File.Move(temporary, DataFile, true);
        }
        catch { }
    }
}
