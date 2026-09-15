using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using VibecoreHub.Desktop.Modules;
using VibecoreHub.Desktop.Platform;

namespace VibecoreHub.Desktop;

public partial class MainWindow : Window
{
    private readonly Dictionary<string, ModuleHost> _openModules = [];
    private readonly Dictionary<string, Button> _moduleButtons = [];
    private NativeTrayIcon? _tray;
    private readonly DispatcherTimer _positionSaveTimer;
    private bool _reallyExit;
    private bool _positionReady;
    private readonly WidgetWindowState _savedWindowState;
    private ModuleHost? _floatingResizeHost;
    private Point _floatingDragStart;
    private double _floatingStartHeight;

    public MainWindow()
    {
        InitializeComponent();
        _savedWindowState = WindowStateStore.Load() ?? new WidgetWindowState();
        Topmost = SettingsStore.Load().AlwaysOnTop;
        UpdatePinState();
        _positionSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _positionSaveTimer.Tick += (_, _) => { _positionSaveTimer.Stop(); if (_positionReady) PersistWindowState(); };
        BuildModuleButtons();
        SourceInitialized += (_, _) => { ApplyRoundedCorners(); _tray = new NativeTrayIcon(this, ShowHub, ExitApp); };
        Loaded += (_, _) => { RestorePosition(); RestoreOpenModules(); };
        LocationChanged += (_, _) => QueueWindowStateSave();
        SizeChanged += (_, e) => { if (e.WidthChanged) QueueWindowStateSave(); };
        Closing += (_, e) =>
        {
            if (_reallyExit) return;
            PersistWindowState();
            e.Cancel = true;
            Hide();
        };
    }

    private void BuildModuleButtons()
    {
        ModuleButtons.Children.Clear();
        _moduleButtons.Clear();
        foreach (var module in OrderedModules())
        {
            var button = CreateModuleButton(module.Glyph, module.Title, true);
            button.Click += (_, _) => ToggleModule(module);
            if (module is IConfigurableHubModule configurable)
                button.MouseRightButtonUp += (_, e) =>
                {
                    e.Handled = true;
                    if (configurable.OpenSettings(this) && _openModules.TryGetValue(module.Id, out var host)) host.ReloadView();
                };
            ModuleButtons.Children.Add(button);
            _moduleButtons[module.Id] = button;
        }
        for (var i = ModuleRegistry.All.Count; i < 4; i++) ModuleButtons.Children.Add(CreateModuleButton($"0{i + 1}", "预留模块", false));
    }

    private static IEnumerable<IHubModule> OrderedModules()
    {
        var settings = SettingsStore.Load();
        return settings.ModuleOrder.Select(id => ModuleRegistry.All.First(module => module.Id == id));
    }

    private Button CreateModuleButton(string content, string tooltip, bool enabled)
    {
        var button = new Button { Style = (Style)FindResource("IconButton"), ToolTip = tooltip, IsEnabled = enabled };
        WindowChrome.SetIsHitTestVisibleInChrome(button, true);
        button.Content = enabled
            ? new TextBlock { FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 17, Text = content }
            : new TextBlock { FontSize = 9, Text = content };
        return button;
    }

    private void ToggleModule(IHubModule module)
    {
        if (_openModules.Remove(module.Id, out var existing))
        {
            _savedWindowState.ModuleHeights[module.Id] = existing.Height;
            ModuleStack.Children.Remove(existing);
        }
        else AddModuleHost(module);
        UpdateButtonState(module.Id);
        SizeToModules();
    }

    private void AddModuleHost(IHubModule module)
    {
        var host = new ModuleHost(module);
        if (_savedWindowState.ModuleHeights.TryGetValue(module.Id, out var savedHeight)) host.Height = Math.Clamp(savedHeight, host.MinHeight, host.MaxHeight);
        host.MaximumVisibleHeight = () => Math.Max(host.MinHeight, AvailableHeightOnCurrentMonitor() - 58 - ModuleStack.Children.OfType<ModuleHost>().Where(other => !ReferenceEquals(other, host)).Sum(other => other.Height));
        host.CloseRequested += (_, _) => ToggleModule(module);
        host.ModuleHeightChanged += (_, _) =>
        {
            _savedWindowState.ModuleHeights[module.Id] = host.Height;
            SizeToModules();
            Dispatcher.BeginInvoke(DispatcherPriority.Render, () => KeepResizeGripVisible(host));
        };
        _openModules[module.Id] = host;
        ModuleStack.Children.Add(host);
    }

    private void UpdateButtonState(string id)
    {
        var active = _openModules.ContainsKey(id);
        var button = _moduleButtons[id];
        button.Background = active ? (Brush)FindResource("RaisedBrush") : Brushes.Transparent;
        button.Foreground = active ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("MutedBrush");
        button.BorderBrush = active ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("LineBrush");
    }

    private void SizeToModules()
    {
        FitModulesOnScreen();
        var desired = 58 + ModuleStack.Children.OfType<ModuleHost>().Sum(host => host.Height);
        Height = Math.Min(desired, AvailableHeightOnCurrentMonitor());
        FloatingResizeGrip.Visibility = ModuleStack.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (_positionReady) { CaptureModuleState(); QueueWindowStateSave(); }
    }

    private void FloatingGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _floatingResizeHost = ModuleStack.Children.OfType<ModuleHost>().LastOrDefault();
        if (_floatingResizeHost is null) return;
        _floatingDragStart = FloatingResizeGrip.PointToScreen(e.GetPosition(FloatingResizeGrip));
        _floatingStartHeight = _floatingResizeHost.Height;
        FloatingResizeGrip.CaptureMouse();
        e.Handled = true;
    }

    private void FloatingGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!FloatingResizeGrip.IsMouseCaptured || _floatingResizeHost is null || e.LeftButton != MouseButtonState.Pressed) return;
        var current = FloatingResizeGrip.PointToScreen(e.GetPosition(FloatingResizeGrip));
        var dpi = VisualTreeHelper.GetDpi(this).DpiScaleY;
        var delta = (current.Y - _floatingDragStart.Y) / Math.Max(1, dpi);
        var maximum = _floatingResizeHost.MaximumVisibleHeight?.Invoke() ?? _floatingResizeHost.MaxHeight;
        _floatingResizeHost.Height = Math.Clamp(_floatingStartHeight + delta, _floatingResizeHost.MinHeight, Math.Max(_floatingResizeHost.MinHeight, Math.Min(_floatingResizeHost.MaxHeight, maximum)));
        _savedWindowState.ModuleHeights[_floatingResizeHost.Module.Id] = _floatingResizeHost.Height;
        SizeToModules();
        e.Handled = true;
    }

    private void FloatingGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (FloatingResizeGrip.IsMouseCaptured) FloatingResizeGrip.ReleaseMouseCapture();
        _floatingResizeHost = null;
        e.Handled = true;
    }

    private void FitModulesOnScreen()
    {
        var hosts = ModuleStack.Children.OfType<ModuleHost>().ToList();
        var excess = hosts.Sum(host => host.Height) - Math.Max(0, AvailableHeightOnCurrentMonitor() - 58);
        if (excess <= 0) return;
        foreach (var host in hosts.AsEnumerable().Reverse())
        {
            var reduction = Math.Min(excess, host.Height - host.MinHeight);
            host.Height -= reduction;
            excess -= reduction;
            if (excess <= 0) break;
        }
    }

    private void KeepResizeGripVisible(ModuleHost host)
    {
        UpdateLayout();
        host.EnsureResizeGripVisible();
        if (ModuleScroller.ViewportHeight <= 0) return;
        var bottom = host.TransformToAncestor(ModuleStack).Transform(new Point(0, host.ActualHeight)).Y;
        var offset = Math.Max(0, bottom - ModuleScroller.ViewportHeight);
        ModuleScroller.ScrollToVerticalOffset(offset);
    }

    private void RestorePosition()
    {
        var saved = _savedWindowState;
        if (saved.Width > 0) Width = Math.Max(MinWidth, saved.Width);
        var visible = saved.Width > 0
            && saved.Left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 40
            && saved.Left + Width > SystemParameters.VirtualScreenLeft + 40
            && saved.Top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 40
            && saved.Top + 58 > SystemParameters.VirtualScreenTop + 20;
        if (visible) { Left = saved.Left; Top = saved.Top; }
        else { var area = SystemParameters.WorkArea; Left = area.Right - Width - 22; Top = area.Top + 22; }
        _positionReady = true;
    }

    private void RestoreOpenModules()
    {
        foreach (var id in _savedWindowState.OpenModuleIds)
        {
            var module = ModuleRegistry.All.FirstOrDefault(candidate => candidate.Id == id);
            if (module is null || _openModules.ContainsKey(id)) continue;
            AddModuleHost(module);
            UpdateButtonState(id);
        }
        SizeToModules();
    }

    private void QueueWindowStateSave()
    {
        if (!_positionReady) return;
        _positionSaveTimer.Stop();
        _positionSaveTimer.Start();
    }

    private void CaptureModuleState()
    {
        _savedWindowState.OpenModuleIds = ModuleStack.Children.OfType<ModuleHost>().Select(host => host.Module.Id).ToList();
        foreach (var host in ModuleStack.Children.OfType<ModuleHost>()) _savedWindowState.ModuleHeights[host.Module.Id] = host.Height;
    }

    private void PersistWindowState()
    {
        if (!_positionReady) return;
        _savedWindowState.Left = Left;
        _savedWindowState.Top = Top;
        _savedWindowState.Width = Width;
        CaptureModuleState();
        WindowStateStore.Save(_savedWindowState);
    }

    private double AvailableHeightOnCurrentMonitor()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var monitor = MonitorFromWindow(handle, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return SystemParameters.WorkArea.Height;
        GetWindowRect(handle, out var windowRect);
        var dpi = VisualTreeHelper.GetDpi(this).DpiScaleY;
        return Math.Max(58, (info.Work.Bottom - windowRect.Top) / dpi - 40);
    }

    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();
    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        var settings = SettingsStore.Load();
        settings.AlwaysOnTop = !settings.AlwaysOnTop;
        SettingsStore.Save(settings);
        Topmost = settings.AlwaysOnTop;
        foreach (Window owned in OwnedWindows) owned.Topmost = Topmost;
        UpdatePinState();
    }

    private void UpdatePinState()
    {
        if (PinButton is null) return;
        PinButton.ToolTip = Topmost ? "取消置顶" : "保持置顶";
        PinButton.Foreground = Topmost ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("MutedBrush");
        PinIcon.Text = Topmost ? "\uE718" : "\uE77A";
    }
    private void LogoArea_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        PersistWindowState();
        var dialog = new SettingsWindow(SettingsStore.Load()) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        if (dialog.ProfileChanged)
        {
            _reallyExit = true;
            _tray?.Dispose();
            var replacement = new MainWindow();
            replacement.Show();
            Close();
            return;
        }
        var openIds = _openModules.Keys.ToHashSet();
        _openModules.Clear();
        ModuleStack.Children.Clear();
        BuildModuleButtons();
        foreach (var module in OrderedModules().Where(module => openIds.Contains(module.Id)))
        {
            AddModuleHost(module);
            UpdateButtonState(module.Id);
        }
        SizeToModules();
    }
    private void ShowHub() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void ExitApp() { PersistWindowState(); _reallyExit = true; _tray?.Dispose(); Close(); System.Windows.Application.Current.Shutdown(); }

    private void ApplyRoundedCorners()
    {
        var preference = 2;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 33, ref preference, sizeof(int));
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] private struct MonitorInfo { public int Size; public NativeRect Monitor; public NativeRect Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
