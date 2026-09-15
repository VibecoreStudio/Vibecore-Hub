using System.Windows;

namespace VibecoreHub.Desktop.Modules;

public interface IHubModule
{
    string Id { get; }
    string Title { get; }
    string Glyph { get; }
    double DefaultHeight { get; }
    double MinHeight { get; }
    double MaxHeight { get; }
    FrameworkElement CreateView();
}

public interface IConfigurableHubModule
{
    bool OpenSettings(Window owner);
}
