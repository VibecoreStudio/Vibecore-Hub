using System.Text.Json.Serialization;

namespace VibecoreHub.Desktop.Models;

public sealed class HubItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "folder";
    public string Name { get; set; } = "";
    public string Target { get; set; } = "";
    public string Color { get; set; } = "amber";
    public long Order { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public string CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToString("O");
    public string? GroupId { get; set; }
    public string? CustomIconPath { get; set; }
    public double GroupWindowWidth { get; set; } = 330;
    public double GroupWindowHeight { get; set; } = 350;

    [JsonIgnore] public string Glyph => Type switch
    {
        "folder" => "\uE8B7",
        "file" => "\uE8A5",
        "application" => "\uECAA",
        "text" => "\uE8D2",
        "group" => "\uE8B7",
        _ => "\uE71D"
    };

    [JsonIgnore] public string TypeLabel => Type switch
    {
        "folder" => "文件夹",
        "file" => "文件",
        "application" => "应用",
        "text" => "单个文本",
        "group" => "收纳文件夹",
        _ => "快捷项"
    };
}

public sealed class HubState
{
    public int Version { get; set; } = 1;
    public string Theme { get; set; } = "midnight";
    public List<HubItem> Items { get; set; } = [];
}
