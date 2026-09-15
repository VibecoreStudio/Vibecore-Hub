namespace VibecoreHub.Desktop.Modules.Audio;

public sealed class AudioPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "音频预设";
    public string OutputDeviceId { get; set; } = "";
    public string OutputDeviceName { get; set; } = "不切换";
    public string InputDeviceId { get; set; } = "";
    public string InputDeviceName { get; set; } = "不切换";
    public int Order { get; set; }
    public string Summary => $"输出：{OutputDeviceName}  ·  输入：{InputDeviceName}";
}

public sealed class AudioPresetState
{
    public List<AudioPreset> Presets { get; set; } = [];
}

public sealed class AudioDeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsInput { get; init; }
    public override string ToString() => Name;
}
