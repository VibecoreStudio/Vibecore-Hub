using System.IO;
using System.Text.Json;

namespace VibecoreHub.Desktop.Modules.Folders;

internal static class FolderDirectoryStore
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static string DataFile => Path.Combine(DataProfileManager.CurrentDirectory, "folder-directories.json");

    public static FolderDirectoryState Load()
    {
        try { return File.Exists(DataFile) ? JsonSerializer.Deserialize<FolderDirectoryState>(File.ReadAllText(DataFile), Options) ?? new() : new(); }
        catch { return new(); }
    }

    public static void Save(FolderDirectoryState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        var temporary = DataFile + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, Options));
        File.Move(temporary, DataFile, true);
    }
}
