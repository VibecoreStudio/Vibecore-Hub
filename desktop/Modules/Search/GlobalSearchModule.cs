using System.Windows;

namespace VibecoreHub.Desktop.Modules.Search;

public sealed class GlobalSearchModule : IHubModule
{
    public string Id => "search";
    public string Title => "全局搜索";
    public string Glyph => "\uE721";
    public double DefaultHeight => 280;
    public double MinHeight => 150;
    public double MaxHeight => 620;
    public FrameworkElement CreateView() => new GlobalSearchModuleView();
}
