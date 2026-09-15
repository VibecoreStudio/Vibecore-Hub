namespace VibecoreHub.Desktop.Modules.Notes;

public sealed class NoteSlot
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int Order { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "未命名" : Title;
    [System.Text.Json.Serialization.JsonIgnore] public string Preview => Content.Replace("\r", " ").Replace("\n", " ");
    [System.Text.Json.Serialization.JsonIgnore] public bool HasContent => !string.IsNullOrWhiteSpace(Content);
}

public sealed class NotesState
{
    public int Version { get; set; } = 1;
    public List<NoteSlot> Slots { get; set; } = [];
    public double ViewerWidth { get; set; } = 340;
    public double ViewerHeight { get; set; } = 300;
}
