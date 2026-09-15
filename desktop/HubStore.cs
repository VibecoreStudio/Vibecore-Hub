using System.IO;
using System.Text.Json;
using VibecoreHub.Desktop.Models;

namespace VibecoreHub.Desktop;

public static class HubStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string DataFile => Path.Combine(DataProfileManager.CurrentDirectory, "hub-state.json");

    public static HubState Load()
    {
        try
        {
            return File.Exists(DataFile)
                ? JsonSerializer.Deserialize<HubState>(File.ReadAllText(DataFile), JsonOptions) ?? NewState()
                : NewState();
        }
        catch { return NewState(); }
    }

    public static void Save(HubState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        var temporary = DataFile + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporary, DataFile, true);
    }

    private static HubState NewState() => new()
    {
        Items =
        [
            new() { Id = "welcome-folder", Type = "folder", Name = "素材库", Target = "右键编辑文件夹路径", Color = "amber", Order = 0 },
            new() { Id = "welcome-app", Type = "application", Name = "常用软件", Target = "右键编辑应用路径", Color = "violet", Order = 1 },
            new() { Id = "welcome-text", Type = "text", Name = "快速回复", Target = "感谢关注，有任何问题随时找我。", Color = "rose", Order = 2 }
        ]
    };
}
