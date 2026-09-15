using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace VibecoreHub.Desktop.Modules.Folders;

public partial class FolderDirectoryModuleView : UserControl
{
    private readonly FolderDirectoryState _state;
    private Point _dragStart;
    private FolderDirectoryItem? _dragged;
    private Adorner? _line;
    private UIElement? _lineTarget;
    private bool _after;
    private DateTime _ignoreClicksUntil;

    public FolderDirectoryModuleView()
    {
        InitializeComponent();
        _state = FolderDirectoryStore.Load();
        Loaded += (_, _) => BuildButtons();
    }

    private void BuildButtons()
    {
        if (FoldersPanel is null) return;
        ClearLine();
        FoldersPanel.Children.Clear();
        var settings = SettingsStore.Load();
        FoldersPanel.Columns = settings.FolderDirectoryColumns;
        var ordered = _state.Items.OrderBy(item => item.Order).ToList();
        foreach (var item in ordered) FoldersPanel.Children.Add(CreateFolderButton(item, settings.FolderDirectoryButtonHeight));
        FoldersPanel.Children.Add(CreateAddButton(settings.FolderDirectoryButtonHeight));
    }

    private Button BaseButton(double height) => new()
    {
        Height = height, Margin = new Thickness(3), HorizontalContentAlignment = HorizontalAlignment.Stretch,
        Background = (Brush)FindResource("WindowBrush"), BorderBrush = (Brush)FindResource("LineBrush"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand,
        Style = (Style)FindResource("FolderButton")
    };

    private Button CreateFolderButton(FolderDirectoryItem item, double height)
    {
        var button = BaseButton(height);
        button.Tag = item;
        button.Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(9, 0, 7, 0),
            Children =
            {
                new TextBlock { Text = "\uE8B7", FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 17, Foreground = (Brush)FindResource("AccentBrush"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,7,0) },
                new TextBlock { Text = item.Name, FontSize = 12, Foreground = (Brush)FindResource("TextBrush"), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis }
            }
        };
        button.Click += Folder_Click;
        button.PreviewMouseLeftButtonDown += Folder_MouseDown;
        button.PreviewMouseMove += Folder_MouseMove;
        button.AllowDrop = true;
        button.DragOver += Folder_DragOver;
        button.Drop += Folder_Drop;
        var menu = new ContextMenu();
        var edit = new MenuItem { Header = "编辑" }; edit.Click += (_, _) => EditItem(item);
        var delete = new MenuItem { Header = "删除" }; delete.Click += (_, _) => { _state.Items.Remove(item); SaveAndRefresh(); };
        menu.Items.Add(edit); menu.Items.Add(delete); button.ContextMenu = menu;
        return button;
    }

    private Button CreateAddButton(double height)
    {
        var button = BaseButton(height);
        button.Content = new TextBlock { Text = "+", FontSize = 20, Foreground = (Brush)FindResource("MutedBrush"), HorizontalAlignment = HorizontalAlignment.Center };
        button.Click += (_, _) => EditItem(null);
        button.AllowDrop = true;
        button.DragOver += Panel_DragOver;
        button.Drop += Panel_Drop;
        return button;
    }

    private void Folder_Click(object sender, RoutedEventArgs e)
    {
        if (DateTime.UtcNow < _ignoreClicksUntil) return;
        _dragged = null;
        try { Process.Start(new ProcessStartInfo(((FolderDirectoryItem)((Button)sender).Tag).Path) { UseShellExecute = true }); }
        catch { System.Media.SystemSounds.Exclamation.Play(); }
    }

    private void EditItem(FolderDirectoryItem? item)
    {
        var editor = new FolderDirectoryEditorWindow(item) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() != true || editor.Result is null) return;
        if (item is null) _state.Items.Add(editor.Result);
        else { var index = _state.Items.IndexOf(item); if (index >= 0) _state.Items[index] = editor.Result; }
        SaveAndRefresh();
    }

    private void Panel_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || !paths.Any(Directory.Exists)) return;
        e.Effects = DragDropEffects.Copy; e.Handled = true;
    }

    private void Panel_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths) return;
        var order = _state.Items.Count;
        foreach (var path in paths.Where(Directory.Exists)) _state.Items.Add(new FolderDirectoryItem { Name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path)), Path = path, Order = order++ });
        SaveAndRefresh(); e.Handled = true;
    }

    private void Folder_MouseDown(object sender, MouseButtonEventArgs e) { _dragStart = e.GetPosition(this); _dragged = (FolderDirectoryItem)((Button)sender).Tag; }
    private void Folder_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragged is null) return;
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(point.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _ignoreClicksUntil = DateTime.UtcNow.AddMilliseconds(350);
        try { DragDrop.DoDragDrop((DependencyObject)sender, new DataObject("VibecoreHub.FolderDirectory", _dragged.Id), DragDropEffects.Move); }
        finally { _dragged = null; ClearLine(); }
    }

    private void Folder_DragOver(object sender, DragEventArgs e)
    {
        if (_dragged is null || !e.Data.GetDataPresent("VibecoreHub.FolderDirectory")) return;
        var button = (Button)sender; var target = (FolderDirectoryItem)button.Tag;
        if (ReferenceEquals(target, _dragged)) return;
        _after = e.GetPosition(button).X > button.ActualWidth / 2;
        ClearLine(); var layer = AdornerLayer.GetAdornerLayer(button); if (layer is null) return;
        _lineTarget = button; _line = new InsertionLineAdorner(button, true, _after); layer.Add(_line);
        e.Effects = DragDropEffects.Move; e.Handled = true;
    }

    private void Folder_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("VibecoreHub.FolderDirectory") || _dragged is null) return;
        var target = (FolderDirectoryItem)((Button)sender).Tag;
        var ordered = _state.Items.OrderBy(item => item.Order).ToList(); ordered.Remove(_dragged);
        ordered.Insert(Math.Clamp(ordered.IndexOf(target) + (_after ? 1 : 0), 0, ordered.Count), _dragged);
        _state.Items = ordered; ClearLine(); SaveAndRefresh(); e.Handled = true;
    }

    private void SaveAndRefresh() { for (var i = 0; i < _state.Items.Count; i++) _state.Items[i].Order = i; FolderDirectoryStore.Save(_state); BuildButtons(); }
    private void ClearLine() { if (_line is not null && _lineTarget is not null) AdornerLayer.GetAdornerLayer(_lineTarget)?.Remove(_line); _line = null; _lineTarget = null; }
}
