using System.Windows;

namespace VibecoreHub.Desktop.Modules.Notes;

public sealed class NotesModule : IHubModule, IConfigurableHubModule
{
    public string Id => "notes";
    public string Title => "便笺卡片";
    public string Glyph => "\uE70B";
    public double DefaultHeight => 300;
    public double MinHeight => Math.Max(58, SettingsStore.Load().NoteRowHeight + 28);
    public double MaxHeight => 680;
    public FrameworkElement CreateView() => new NotesModuleView();
    public bool OpenSettings(Window owner) => new NotesSettingsWindow(SettingsStore.Load()) { Owner = owner }.ShowDialog() == true;
}
