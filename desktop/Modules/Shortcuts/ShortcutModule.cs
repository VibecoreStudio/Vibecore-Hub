using System.Windows;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

public sealed class ShortcutModule : IHubModule, IConfigurableHubModule
{
    public string Id => "shortcuts";
    public string Title => "快捷方式库";
    public string Glyph => "\uE80F";
    public double DefaultHeight => 316;
    public double MinHeight
    {
        get
        {
            var settings = SettingsStore.Load();
            return settings.ShortcutIconSize * 2 + 60 + settings.ShortcutGap * 2 + 24;
        }
    }
    public double MaxHeight => 680;
    public FrameworkElement CreateView() => new ShortcutModuleView();
    public bool OpenSettings(Window owner) => new ShortcutSettingsWindow(SettingsStore.Load()) { Owner = owner }.ShowDialog() == true;
}
