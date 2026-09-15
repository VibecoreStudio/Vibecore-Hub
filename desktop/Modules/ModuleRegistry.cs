using VibecoreHub.Desktop.Modules.Shortcuts;
using VibecoreHub.Desktop.Modules.Notes;
using VibecoreHub.Desktop.Modules.Search;
using VibecoreHub.Desktop.Modules.Folders;
using VibecoreHub.Desktop.Modules.Audio;

namespace VibecoreHub.Desktop.Modules;

public static class ModuleRegistry
{
    // 新功能只需要实现 IHubModule，并在此添加一行。
    public static IReadOnlyList<IHubModule> All { get; } =
    [
        new GlobalSearchModule(),
        new ShortcutModule(),
        new NotesModule(),
        new FolderDirectoryModule(),
        new AudioSwitcherModule()
    ];
}
