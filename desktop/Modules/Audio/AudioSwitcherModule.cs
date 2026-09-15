using System.Windows;

namespace VibecoreHub.Desktop.Modules.Audio;

public sealed class AudioSwitcherModule : IHubModule
{
    public string Id => "audio-switcher";
    public string Title => "音频切换";
    public string Glyph => "\uE767";
    public double DefaultHeight => 225;
    public double MinHeight => 124;
    public double MaxHeight => 520;
    public FrameworkElement CreateView() => new AudioSwitcherModuleView();
}
