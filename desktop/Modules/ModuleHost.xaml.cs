using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VibecoreHub.Desktop.Modules;

public partial class ModuleHost : UserControl
{
    private Point _dragStart;
    private double _startHeight;

    public IHubModule Module { get; }
    public event EventHandler? CloseRequested;
    public event EventHandler? ModuleHeightChanged;
    public Func<double>? MaximumVisibleHeight { get; set; }

    public ModuleHost(IHubModule module)
    {
        InitializeComponent();
        Module = module;
        Height = module.DefaultHeight;
        MinHeight = module.MinHeight;
        MaxHeight = module.MaxHeight;
        ModuleContent.Content = module.CreateView();
    }

    public void ReloadView() => ModuleContent.Content = Module.CreateView();
    public void EnsureResizeGripVisible() => ResizeGrip.BringIntoView();

    private void Close_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = ResizeGrip.PointToScreen(e.GetPosition(ResizeGrip));
        _startHeight = Height;
        ResizeGrip.CaptureMouse();
        Mouse.AddMouseMoveHandler(ResizeGrip, ResizeGrip_MouseMove);
        Mouse.AddMouseUpHandler(ResizeGrip, ResizeGrip_MouseUp);
        e.Handled = true;
    }

    private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!ResizeGrip.IsMouseCaptured) return;
        var current = ResizeGrip.PointToScreen(e.GetPosition(ResizeGrip));
        var dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleY;
        var delta = (current.Y - _dragStart.Y) / Math.Max(1, dpiScale);
        var visibleMaximum = MaximumVisibleHeight?.Invoke() ?? MaxHeight;
        Height = Math.Clamp(_startHeight + delta, MinHeight, Math.Max(MinHeight, Math.Min(MaxHeight, visibleMaximum)));
        ModuleHeightChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ResizeGrip_MouseUp(object sender, MouseButtonEventArgs e)
    {
        ResizeGrip.ReleaseMouseCapture();
        Mouse.RemoveMouseMoveHandler(ResizeGrip, ResizeGrip_MouseMove);
        Mouse.RemoveMouseUpHandler(ResizeGrip, ResizeGrip_MouseUp);
        ModuleHeightChanged?.Invoke(this, EventArgs.Empty);
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, EnsureResizeGripVisible);
    }
}
