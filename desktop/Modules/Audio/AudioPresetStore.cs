using System.IO;
using System.Text.Json;

namespace VibecoreHub.Desktop.Modules.Audio;

internal static class AudioPresetStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    private static string DataFile => Path.Combine(DataProfileManager.CurrentDirectory, "audio-presets.json");

    public static AudioPresetState Load()
    {
        try
        {
            if (File.Exists(DataFile))
                return JsonSerializer.Deserialize<AudioPresetState>(File.ReadAllText(DataFile), Options) ?? new();
        }
        catch { }
        return new();
    }

    public static void Save(AudioPresetState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        var temporary = DataFile + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, Options));
        File.Move(temporary, DataFile, true);
    }
}
