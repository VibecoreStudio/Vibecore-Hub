using System.Windows;

namespace VibecoreHub.Desktop.Modules.Folders;

public sealed class FolderDirectoryModule : IHubModule, IConfigurableHubModule
{
    public string Id => "folders";
    public string Title => "文件夹目录";
    public string Glyph => "\uE8B7";
    public double DefaultHeight => 250;
    public double MinHeight => Math.Max(66, SettingsStore.Load().FolderDirectoryButtonHeight + 32);
    public double MaxHeight => 680;
    public FrameworkElement CreateView() => new FolderDirectoryModuleView();
    public bool OpenSettings(Window owner) => new FolderDirectorySettingsWindow(SettingsStore.Load()) { Owner = owner }.ShowDialog() == true;
}
