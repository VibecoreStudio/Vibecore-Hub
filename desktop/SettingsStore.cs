using System.IO;
using System.Text.Json;
using VibecoreHub.Desktop.Modules;

namespace VibecoreHub.Desktop;

public sealed class AppSettings
{
    public string Theme { get; set; } = "midnight";
    public List<string> ModuleOrder { get; set; } = [];
    public int ShortcutColumns { get; set; } = 4;
    public int ShortcutGap { get; set; } = 8;
    public int ShortcutIconSize { get; set; } = 54;
    public string ShortcutGroupOpenMode { get; set; } = "single";
    public int NoteRowHeight { get; set; } = 34;
    public int NoteColumns { get; set; } = 1;
    public string NoteOpenMode { get; set; } = "single";
    public bool AlwaysOnTop { get; set; } = true;
    public int FolderDirectoryColumns { get; set; } = 2;
    public int FolderDirectoryButtonHeight { get; set; } = 46;
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    private static string DataFile => Path.Combine(DataProfileManager.CurrentDirectory, "settings.json");

    public static AppSettings Load()
    {
        AppSettings settings;
        try { settings = File.Exists(DataFile) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(DataFile), Options) ?? new() : new(); }
        catch { settings = new(); }
        var known = ModuleRegistry.All.Select(module => module.Id).ToList();
        settings.ModuleOrder = settings.ModuleOrder.Where(known.Contains).Concat(known.Where(id => !settings.ModuleOrder.Contains(id))).ToList();
        settings.ShortcutColumns = Math.Clamp(settings.ShortcutColumns, 2, 8);
        settings.ShortcutGap = Math.Clamp(settings.ShortcutGap, 0, 24);
        settings.ShortcutIconSize = Math.Clamp(settings.ShortcutIconSize, 30, 72);
        settings.ShortcutGroupOpenMode = settings.ShortcutGroupOpenMode == "multiple" ? "multiple" : "single";
        settings.NoteRowHeight = Math.Clamp(settings.NoteRowHeight, 28, 60);
        settings.NoteColumns = Math.Clamp(settings.NoteColumns, 1, 3);
        settings.NoteOpenMode = settings.NoteOpenMode == "multiple" ? "multiple" : "single";
        settings.FolderDirectoryColumns = Math.Clamp(settings.FolderDirectoryColumns, 2, 3);
        settings.FolderDirectoryButtonHeight = Math.Clamp(settings.FolderDirectoryButtonHeight, 34, 72);
        return settings;
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        File.WriteAllText(DataFile, JsonSerializer.Serialize(settings, Options));
    }
}
