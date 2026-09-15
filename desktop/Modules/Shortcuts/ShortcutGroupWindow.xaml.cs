using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using VibecoreHub.Desktop.Models;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

public partial class ShortcutGroupWindow : Window
{
    private readonly HubItem _group;
    private readonly HubState _state;
    private readonly Action _save;
    private Point _dragStart;
    private HubItem? _dragged;
    private Adorner? _line;
    private UIElement? _lineTarget;
    private bool _after;
    private DateTime _ignoreClicksUntil;
    private bool _positionReady;
    public string GroupId => _group.Id;

    public ShortcutGroupWindow(HubItem group, HubState state, Action save)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        _group = group;
        _state = state;
        _save = save;
        Width = Math.Max(MinWidth, group.GroupWindowWidth);
        Height = Math.Max(MinHeight, group.GroupWindowHeight);
        TitleText.Text = group.Name;
        Loaded += (_, _) =>
        {
            if (Owner is not null) { Topmost = Owner.Topmost; Owner.LocationChanged += OwnerMoved; }
            _positionReady = true;
            AttachToOwner();
            BuildItems();
        };
        SizeChanged += (_, _) => { if (_positionReady) AttachToOwner(); };
        Closed += (_, _) =>
        {
            if (Owner is not null) Owner.LocationChanged -= OwnerMoved;
            _group.GroupWindowWidth = ActualWidth;
            _group.GroupWindowHeight = ActualHeight;
            _save();
        };
    }

    private void BuildItems()
    {
        ClearLine();
        ItemsPanel.Children.Clear();
        foreach (var item in _state.Items.Where(item => item.GroupId == _group.Id).OrderBy(item => item.Order)) ItemsPanel.Children.Add(CreateTile(item));
    }

    private Button CreateTile(HubItem item)
    {
        var button = new Button { Width = 68, Height = 84, Margin = new Thickness(3), Tag = item, Style = (Style)FindResource("TileButton"), AllowDrop = true };
        button.Click += Item_Click;
        button.PreviewMouseLeftButtonDown += Item_MouseDown;
        button.PreviewMouseMove += Item_MouseMove;
        button.DragOver += Item_DragOver;
        button.Drop += Item_Drop;
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        var box = new Border { Width = 52, Height = 52, CornerRadius = new CornerRadius(14), Background = (Brush)FindResource("RaisedBrush"), BorderBrush = (Brush)FindResource("LineBrush"), BorderThickness = new Thickness(1) };
        var grid = new Grid();
        var imageSource = IconService.GetItemIcon(item);
        if (imageSource is not null)
        {
            box.Background = Brushes.Transparent;
            box.BorderThickness = new Thickness(0);
            grid.Children.Add(new Image { Source = imageSource, Width = 52, Height = 52, Stretch = Stretch.UniformToFill });
        }
        else grid.Children.Add(new TextBlock { Text = item.Glyph, FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 24, Foreground = (Brush)FindResource("AccentBrush"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        box.Child = grid;
        stack.Children.Add(box);
        stack.Children.Add(new TextBlock { Text = item.Name, Width = 64, Margin = new Thickness(0, 4, 0, 0), FontSize = 11, Foreground = (Brush)FindResource("TextBrush"), TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
        button.Content = stack;
        var menu = new ContextMenu();
        var edit = new MenuItem { Header = "编辑" };
        edit.Click += (_, _) =>
        {
            var editor = new ShortcutEditorWindow(item) { Owner = this };
            if (editor.ShowDialog() != true || editor.Result is null) return;
            var index = _state.Items.IndexOf(item);
            if (index >= 0) _state.Items[index] = editor.Result;
            SaveAndRefresh();
        };
        var setIcon = new MenuItem { Header = "设置图标" };
        setIcon.Click += (_, _) => { if (CustomIconManager.Choose(item, this)) SaveAndRefresh(); };
        var resetIcon = new MenuItem { Header = "还原默认图标", IsEnabled = !string.IsNullOrWhiteSpace(item.CustomIconPath) };
        resetIcon.Click += (_, _) => { CustomIconManager.RestoreDefault(item); SaveAndRefresh(); };
        var moveOut = new MenuItem { Header = "移出收纳文件夹" };
        moveOut.Click += (_, _) => { item.GroupId = null; item.Order = NextTopLevelOrder(); SaveAndRefresh(); };
        var delete = new MenuItem { Header = "删除" };
        delete.Click += (_, _) => { _state.Items.Remove(item); SaveAndRefresh(); };
        menu.Items.Add(edit); menu.Items.Add(setIcon); menu.Items.Add(resetIcon); menu.Items.Add(moveOut); menu.Items.Add(delete);
        button.ContextMenu = menu;
        return button;
    }

    private void Item_Click(object sender, RoutedEventArgs e)
    {
        if (DateTime.UtcNow < _ignoreClicksUntil) return;
        _dragged = null;
        var item = (HubItem)((Button)sender).Tag;
        try
        {
            if (item.Type == "text") Clipboard.SetText(item.Target);
            else if (item.Type == "application") ShortcutTarget.Launch(item.Target);
            else Process.Start(new ProcessStartInfo(ShortcutTarget.Normalize(item.Target, item.Type)) { UseShellExecute = true });
        }
        catch { System.Media.SystemSounds.Exclamation.Play(); }
    }

    private void Item_MouseDown(object sender, MouseButtonEventArgs e) { _dragStart = e.GetPosition(this); _dragged = (HubItem)((Button)sender).Tag; }

    private void Item_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragged is null) return;
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(point.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _ignoreClicksUntil = DateTime.UtcNow.AddMilliseconds(350);
        try { DragDrop.DoDragDrop((DependencyObject)sender, new DataObject("VibecoreHub.Shortcut", _dragged.Id), DragDropEffects.Move); }
        finally { _dragged = null; ClearLine(); }
    }

    private void Item_DragOver(object sender, DragEventArgs e)
    {
        if (_dragged is null || !e.Data.GetDataPresent("VibecoreHub.Shortcut")) return;
        var target = (HubItem)((Button)sender).Tag;
        if (ReferenceEquals(target, _dragged)) { ClearLine(); return; }
        var button = (Button)sender;
        _after = e.GetPosition(button).X > button.ActualWidth / 2;
        ShowLine(button, _after);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Item_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("VibecoreHub.Shortcut") || _dragged is null) return;
        var target = (HubItem)((Button)sender).Tag;
        var ordered = _state.Items.Where(item => item.GroupId == _group.Id).OrderBy(item => item.Order).ToList();
        ordered.Remove(_dragged);
        ordered.Insert(Math.Clamp(ordered.IndexOf(target) + (_after ? 1 : 0), 0, ordered.Count), _dragged);
        for (var index = 0; index < ordered.Count; index++) ordered[index].Order = index;
        ClearLine();
        SaveAndRefresh();
        e.Handled = true;
    }

    private void Panel_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop) && !e.Data.GetDataPresent("VibecoreHub.Shortcut")) return;
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.Move;
        e.Handled = true;
    }

    private void Panel_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            var order = NextGroupOrder();
            foreach (var path in paths)
            {
                var item = DroppedShortcutFactory.Create(path, order++);
                if (item is null) continue;
                item.GroupId = _group.Id;
                _state.Items.Add(item);
            }
            SaveAndRefresh();
        }
        else if (e.Data.GetData("VibecoreHub.Shortcut") is string id)
        {
            var item = _state.Items.FirstOrDefault(candidate => candidate.Id == id && candidate.Type != "group");
            if (item is not null) { item.GroupId = _group.Id; item.Order = NextGroupOrder(); SaveAndRefresh(); }
        }
        e.Handled = true;
    }

    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (_dragged is not null || e.Data.GetData("VibecoreHub.Shortcut") is not string id) return;
        var item = _state.Items.FirstOrDefault(candidate => candidate.Id == id);
        if (item is null || item.Type == "group" || item.GroupId == _group.Id) return;
        ShowPanelDropTarget();
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Window_PreviewDrop(object sender, DragEventArgs e)
    {
        if (_dragged is not null || e.Data.GetData("VibecoreHub.Shortcut") is not string id) return;
        var item = _state.Items.FirstOrDefault(candidate => candidate.Id == id);
        if (item is null || item.Type == "group" || item.GroupId == _group.Id) return;
        item.GroupId = _group.Id;
        item.Order = NextGroupOrder();
        ClearLine();
        SaveAndRefresh();
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private long NextGroupOrder() => _state.Items.Where(item => item.GroupId == _group.Id).Select(item => item.Order).DefaultIfEmpty(-1).Max() + 1;
    private long NextTopLevelOrder() => _state.Items.Where(item => string.IsNullOrEmpty(item.GroupId)).Select(item => item.Order).DefaultIfEmpty(-1).Max() + 1;
    private void SaveAndRefresh() { ClearLine(); _save(); BuildItems(); }

    private void ShowLine(UIElement target, bool after)
    {
        ClearLine();
        var layer = AdornerLayer.GetAdornerLayer(target);
        if (layer is null) return;
        _lineTarget = target;
        _line = new InsertionLineAdorner(target, true, after);
        layer.Add(_line);
    }

    private void ShowPanelDropTarget()
    {
        if (ReferenceEquals(_lineTarget, ItemsPanel) && _line is DropTargetAdorner) return;
        ClearLine();
        var layer = AdornerLayer.GetAdornerLayer(ItemsPanel);
        if (layer is null) return;
        _lineTarget = ItemsPanel;
        _line = new DropTargetAdorner(ItemsPanel);
        layer.Add(_line);
    }

    private void ClearLine()
    {
        if (_line is not null && _lineTarget is not null) AdornerLayer.GetAdornerLayer(_lineTarget)?.Remove(_line);
        _line = null; _lineTarget = null;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void OwnerMoved(object? sender, EventArgs e) => AttachToOwner();
    private void AttachToOwner() { if (Owner is not null) { Left = Owner.Left - ActualWidth; Top = Owner.Top; } }
}
