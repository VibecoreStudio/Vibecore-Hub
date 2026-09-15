namespace VibecoreHub.Desktop.Modules.Folders;

public sealed class FolderDirectoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public int Order { get; set; }
}

public sealed class FolderDirectoryState
{
    public int Version { get; set; } = 1;
    public List<FolderDirectoryItem> Items { get; set; } = [];
}
