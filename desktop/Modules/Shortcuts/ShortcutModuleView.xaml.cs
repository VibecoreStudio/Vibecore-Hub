using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Threading;
using VibecoreHub.Desktop.Models;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

public partial class ShortcutModuleView : UserControl
{
    private HubState _state;
    private Point _dragStart;
    private HubItem? _draggedItem;
    private Adorner? _insertionLine;
    private UIElement? _insertionTarget;
    private bool _insertAfter;
    private bool _lineAfter;
    private bool _dropIntoGroup;
    private DateTime _ignoreClicksUntil;
    private readonly DispatcherTimer _resizeTimer;

    public ShortcutModuleView()
    {
        InitializeComponent();
        _state = HubStore.Load();
        _resizeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _resizeTimer.Tick += (_, _) => { _resizeTimer.Stop(); BuildTiles(); };
        Loaded += (_, _) => BuildTiles();
        SizeChanged += (_, e) =>
        {
            if (!e.WidthChanged || !IsLoaded) return;
            _resizeTimer.Stop();
            _resizeTimer.Start();
        };
        Unloaded += (_, _) => _resizeTimer.Stop();
    }

    private void BuildTiles()
    {
        ClearInsertionLine();
        TilesPanel.Children.Clear();
        var settings = SettingsStore.Load();
        var items = _state.Items.Where(item => string.IsNullOrEmpty(item.GroupId)).OrderBy(item => item.Order).ToList();
        // Base the grid on the viewport width, never on the panel's previous desired size.
        // Rounding down leaves a tiny safety margin and prevents boundary re-wrapping.
        var available = Math.Max(120, ActualWidth - 26);
        TilesPanel.Width = available;
        var rawTileWidth = (available - settings.ShortcutGap * (settings.ShortcutColumns - 1)) / settings.ShortcutColumns;
        var tileWidth = Math.Max(32, Math.Floor(rawTileWidth * 2) / 2);
        var iconSize = Math.Min(settings.ShortcutIconSize, Math.Max(30, tileWidth - 4));
        for (var index = 0; index < items.Count; index++)
            TilesPanel.Children.Add(CreateTile(items[index], tileWidth, iconSize, (index + 1) % settings.ShortcutColumns == 0 ? 0 : settings.ShortcutGap, settings.ShortcutGap));
        var addIndex = items.Count;
        TilesPanel.Children.Add(CreateAddTile(tileWidth, iconSize, (addIndex + 1) % settings.ShortcutColumns == 0 ? 0 : settings.ShortcutGap, settings.ShortcutGap));
    }

    private Button CreateTile(HubItem item, double width, double iconSize, double rightGap, double bottomGap)
    {
        var tile = BaseTile(width, iconSize, rightGap, bottomGap);
        tile.Tag = item;
        tile.Click += Shortcut_Click;
        EnableReorder(tile);
        var menu = new ContextMenu();
        var edit = new MenuItem { Header = "编辑" };
        edit.Click += (_, _) => EditItem(item);
        var setIcon = new MenuItem { Header = "设置图标" };
        setIcon.Click += (_, _) => { if (CustomIconManager.Choose(item, Window.GetWindow(this))) { HubStore.Save(_state); BuildTiles(); } };
        var resetIcon = new MenuItem { Header = "还原默认图标", IsEnabled = !string.IsNullOrWhiteSpace(item.CustomIconPath) };
        resetIcon.Click += (_, _) => { CustomIconManager.RestoreDefault(item); HubStore.Save(_state); BuildTiles(); };
        var delete = new MenuItem { Header = "删除" };
        delete.Click += (_, _) => DeleteItem(item);
        menu.Items.Add(edit); menu.Items.Add(setIcon); menu.Items.Add(resetIcon); menu.Items.Add(delete);
        tile.ContextMenu = menu;

        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        var iconBox = new Border
        {
            Width = iconSize, Height = iconSize, CornerRadius = new CornerRadius(Math.Min(15, iconSize / 3.6)),
            Background = (Brush)FindResource("RaisedBrush"), BorderBrush = (Brush)FindResource("LineBrush"), BorderThickness = new Thickness(1)
        };
        var iconGrid = new Grid();
        var appIcon = IconService.GetItemIcon(item);
        if (appIcon is not null)
        {
            iconBox.Background = Brushes.Transparent;
            iconBox.BorderThickness = new Thickness(0);
            var image = new Image { Source = appIcon, Width = iconSize, Height = iconSize, Stretch = Stretch.UniformToFill };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            iconGrid.Children.Add(image);
        }
        else
        {
            iconGrid.Children.Add(new TextBlock
            {
                Text = item.Glyph, FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = Math.Clamp(iconSize * 0.46, 15, 25),
                Foreground = AccentFor(item.Color), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            });
        }
        iconBox.Child = iconGrid;
        stack.Children.Add(iconBox);
        stack.Children.Add(new TextBlock
        {
            Text = item.Name, Width = Math.Max(50, width - 4), Margin = new Thickness(0, 5, 0, 0), FontSize = 12,
            Foreground = (Brush)FindResource("TextBrush"), TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        tile.Content = stack;
        return tile;
    }

    private Button CreateAddTile(double width, double iconSize, double rightGap, double bottomGap)
    {
        var tile = BaseTile(width, iconSize, rightGap, bottomGap);
        tile.Click += (_, _) => EditItem(null);
        tile.AllowDrop = true;
        tile.DragOver += AddTile_DragOver;
        tile.Drop += Tile_Drop;
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(new Border
        {
            Width = iconSize, Height = iconSize, CornerRadius = new CornerRadius(Math.Min(15, iconSize / 3.6)), BorderBrush = (Brush)FindResource("FaintBrush"), BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = "+", FontSize = Math.Clamp(iconSize * 0.46, 15, 25), Foreground = (Brush)FindResource("MutedBrush"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        });
        stack.Children.Add(new TextBlock { Text = "添加", Width = Math.Max(50, width - 4), Margin = new Thickness(0, 5, 0, 0), FontSize = 12, Foreground = (Brush)FindResource("MutedBrush"), TextAlignment = TextAlignment.Center });
        tile.Content = stack;
        return tile;
    }

    private static Button BaseTile(double width, double iconSize, double rightGap, double bottomGap) => new()
    {
        Width = width, Height = iconSize + 30, Margin = new Thickness(0, 0, rightGap, bottomGap), Padding = new Thickness(0),
        Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
        Style = (Style)System.Windows.Application.Current.FindResource("TileButton")
    };

    private Brush AccentFor(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(color switch
    {
        "sky" => "#69C7FF", "violet" => "#A28AFF", "rose" => "#FF7E9F", _ => "#FFBD59"
    }));

    private void Shortcut_Click(object sender, RoutedEventArgs e)
    {
        if (DateTime.UtcNow < _ignoreClicksUntil) { e.Handled = true; return; }
        _draggedItem = null;
        var item = (HubItem)((Button)sender).Tag;
        if (item.Type == "group")
        {
            ShortcutGroupWindowLauncher.Open(Window.GetWindow(this), item, _state, () => { HubStore.Save(_state); BuildTiles(); });
            return;
        }
        try
        {
            if (item.Type == "text") Clipboard.SetText(item.Target);
            else if (item.Type == "application") ShortcutTarget.Launch(item.Target);
            else Process.Start(new ProcessStartInfo(ShortcutTarget.Normalize(item.Target, item.Type)) { UseShellExecute = true });
        }
        catch { System.Media.SystemSounds.Exclamation.Play(); }
    }

    private void EnableReorder(Button tile)
    {
        tile.AllowDrop = true;
        tile.PreviewMouseLeftButtonDown += Tile_PreviewMouseLeftButtonDown;
        tile.PreviewMouseMove += Tile_PreviewMouseMove;
        tile.DragOver += Tile_DragOver;
        tile.Drop += Tile_Drop;
    }

    private void Tile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _draggedItem = (HubItem)((Button)sender).Tag;
    }

    private void Tile_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedItem is null) return;
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(point.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _ignoreClicksUntil = DateTime.UtcNow.AddMilliseconds(350);
        var data = new DataObject("VibecoreHub.Shortcut", _draggedItem.Id);
        try { DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move); }
        finally { _draggedItem = null; ClearInsertionLine(); }
    }

    private void Tile_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("VibecoreHub.Shortcut") || _draggedItem is null) return;
        var tile = (Button)sender;
        var target = tile.Tag as HubItem;
        if (ReferenceEquals(target, _draggedItem)) { ClearInsertionLine(); return; }
        var relativeX = e.GetPosition(tile).X / Math.Max(1, tile.ActualWidth);
        if (target?.Type == "group" && _draggedItem.Type != "group" && relativeX is > 0.22 and < 0.78)
        {
            _dropIntoGroup = true;
            ShowDropTarget(tile);
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            return;
        }
        _dropIntoGroup = false;
        _insertAfter = e.GetPosition(tile).X > tile.ActualWidth / 2;
        ShowInsertionLine(tile, true, _insertAfter);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void AddTile_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            ShowDropTarget((Button)sender);
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            return;
        }
        if (!e.Data.GetDataPresent("VibecoreHub.Shortcut") || _draggedItem is null) return;
        _insertAfter = false;
        ShowInsertionLine((Button)sender, true, false);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Tile_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("VibecoreHub.Shortcut") || _draggedItem is null) return;
        var target = ((Button)sender).Tag as HubItem;
        if (ReferenceEquals(target, _draggedItem)) return;
        if (_dropIntoGroup && target?.Type == "group" && _draggedItem.Type != "group")
        {
            _draggedItem.GroupId = target.Id;
            _draggedItem.Order = _state.Items.Where(item => item.GroupId == target.Id).Select(item => item.Order).DefaultIfEmpty(-1).Max() + 1;
            HubStore.Save(_state);
            ClearInsertionLine();
            BuildTiles();
            e.Handled = true;
            return;
        }
        var ordered = _state.Items.Where(item => string.IsNullOrEmpty(item.GroupId)).OrderBy(item => item.Order).ToList();
        ordered.Remove(_draggedItem);
        var index = target is null ? ordered.Count : ordered.IndexOf(target) + (_insertAfter ? 1 : 0);
        ordered.Insert(Math.Clamp(index, 0, ordered.Count), _draggedItem);
        for (var position = 0; position < ordered.Count; position++) ordered[position].Order = position;
        HubStore.Save(_state);
        ClearInsertionLine();
        BuildTiles();
        e.Handled = true;
    }

    private void ShowInsertionLine(UIElement target, bool vertical, bool after)
    {
        if (ReferenceEquals(_insertionTarget, target) && _insertionLine is not null && _lineAfter == after) return;
        ClearInsertionLine();
        var layer = AdornerLayer.GetAdornerLayer(target);
        if (layer is null) return;
        _insertionTarget = target;
        _lineAfter = after;
        _insertionLine = new InsertionLineAdorner(target, vertical, after);
        layer.Add(_insertionLine);
    }

    private void ShowDropTarget(UIElement target)
    {
        ClearInsertionLine();
        var layer = AdornerLayer.GetAdornerLayer(target);
        if (layer is null) return;
        _insertionTarget = target;
        _insertionLine = new DropTargetAdorner(target);
        layer.Add(_insertionLine);
    }

    private void ClearInsertionLine()
    {
        if (_insertionLine is not null && _insertionTarget is not null)
            AdornerLayer.GetAdornerLayer(_insertionTarget)?.Remove(_insertionLine);
        _insertionLine = null;
        _insertionTarget = null;
    }

    private void ExternalFiles_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void ExternalFiles_PreviewDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths) return;
        ImportPaths(paths);
        ClearInsertionLine();
        e.Handled = true;
    }

    private void ImportPaths(IEnumerable<string> paths)
    {
        var nextOrder = _state.Items.Count == 0 ? 0 : _state.Items.Max(item => item.Order) + 1;
        foreach (var path in paths)
        {
            var item = DroppedShortcutFactory.Create(path, nextOrder++);
            if (item is not null) _state.Items.Add(item);
        }
        HubStore.Save(_state);
        BuildTiles();
    }

    private void EditItem(HubItem? item)
    {
        var editor = new ShortcutEditorWindow(item) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() != true || editor.Result is null) return;
        if (item is null) _state.Items.Add(editor.Result);
        else
        {
            var index = _state.Items.IndexOf(item);
            if (index >= 0) _state.Items[index] = editor.Result;
        }
        HubStore.Save(_state); BuildTiles();
    }

    private void DeleteItem(HubItem item)
    {
        if (item.Type == "group")
        {
            var next = _state.Items.Where(candidate => string.IsNullOrEmpty(candidate.GroupId) && candidate.Id != item.Id).Select(candidate => candidate.Order).DefaultIfEmpty(-1).Max() + 1;
            foreach (var child in _state.Items.Where(candidate => candidate.GroupId == item.Id)) { child.GroupId = null; child.Order = next++; }
        }
        _state.Items.Remove(item); HubStore.Save(_state); BuildTiles();
    }

}
