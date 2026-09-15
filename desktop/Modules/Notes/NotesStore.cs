using System.IO;
using System.Text.Json;

namespace VibecoreHub.Desktop.Modules.Notes;

internal static class NotesStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static string DataFile => Path.Combine(DataProfileManager.CurrentDirectory, "notes.json");

    public static NotesState Load()
    {
        try
        {
            if (File.Exists(DataFile))
            {
                var state = JsonSerializer.Deserialize<NotesState>(File.ReadAllText(DataFile), Options) ?? NewState();
                state.Slots = state.Slots.Where(slot => !string.IsNullOrWhiteSpace(slot.Title) || !string.IsNullOrWhiteSpace(slot.Content)).ToList();
                for (var index = 0; index < state.Slots.Count; index++) state.Slots[index].Order = index;
                return state;
            }
        }
        catch { }
        return NewState();
    }

    public static void Save(NotesState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        var temporary = DataFile + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, Options));
        File.Move(temporary, DataFile, true);
    }

    private static NotesState NewState() => new();
}
