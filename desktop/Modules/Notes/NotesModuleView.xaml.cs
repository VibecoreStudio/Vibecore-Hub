using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace VibecoreHub.Desktop.Modules.Notes;

public partial class NotesModuleView : UserControl
{
    private readonly NotesState _state;
    private Point _dragStart;
    private NoteSlot? _draggedSlot;
    private InsertionLineAdorner? _insertionLine;
    private UIElement? _insertionTarget;
    private bool _insertAfter;
    private DateTime _ignoreClicksUntil;

    public NotesModuleView()
    {
        InitializeComponent();
        _state = NotesStore.Load();
        Loaded += (_, _) => Refresh();
        SizeChanged += (_, e) => { if (e.WidthChanged && IsLoaded) Refresh(); };
    }

    private void Refresh()
    {
        ClearInsertionLine();
        NotesPanel.Children.Clear();
        var settings = SettingsStore.Load();
        var columns = settings.NoteColumns;
        var gap = 4d;
        var availableWidth = NotesPanel.ActualWidth > 80 ? NotesPanel.ActualWidth : Math.Max(200, ActualWidth - 26);
        var cardWidth = Math.Max(58, (availableWidth - gap * (columns - 1)) / columns);
        var ordered = _state.Slots.OrderBy(slot => slot.Order).ToList();
        for (var index = 0; index < ordered.Count; index++)
            NotesPanel.Children.Add(CreateNoteButton(ordered[index], cardWidth, settings.NoteRowHeight, index % columns < columns - 1));
        NotesPanel.Children.Add(CreateAddButton(cardWidth, settings.NoteRowHeight, ordered.Count % columns < columns - 1));
    }

    private Button CreateNoteButton(NoteSlot slot, double width, double height, bool rightGap)
    {
        var button = new Button
        {
            Width = width,
            Height = height,
            Margin = new Thickness(0, 0, rightGap ? 4 : 0, 4),
            Style = (Style)FindResource("NoteRow"),
            Tag = slot,
            AllowDrop = true
        };
        button.Click += Slot_Click;
        button.MouseRightButtonUp += Slot_RightClick;
        button.PreviewMouseLeftButtonDown += Slot_PreviewMouseLeftButtonDown;
        button.PreviewMouseMove += Slot_PreviewMouseMove;
        button.DragOver += Slot_DragOver;
        button.Drop += Slot_Drop;

        var content = new Grid { ClipToBounds = true };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.ColumnDefinitions.Add(new ColumnDefinition());
        var title = new TextBlock
        {
            Text = slot.DisplayTitle,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var preview = new TextBlock
        {
            Text = slot.Preview,
            Margin = new Thickness(12, 0, 0, 0),
            FontSize = 12,
            Foreground = (Brush)FindResource("FaintBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(preview, 1);
        content.Children.Add(title);
        content.Children.Add(preview);
        button.Content = content;
        return button;
    }

    private Button CreateAddButton(double width, double height, bool rightGap)
    {
        var button = new Button
        {
            Width = width,
            Height = height,
            Margin = new Thickness(0, 0, rightGap ? 4 : 0, 4),
            Style = (Style)FindResource("NoteRow"),
            ToolTip = "添加便笺",
            AllowDrop = true,
            Content = new TextBlock
            {
                Text = "+",
                FontSize = 20,
                FontWeight = FontWeights.Light,
                Foreground = (Brush)FindResource("MutedBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        button.Click += Add_Click;
        button.DragOver += Add_DragOver;
        button.Drop += Slot_Drop;
        return button;
    }

    private void Slot_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var slot = (NoteSlot)((Button)sender).Tag;
        var button = (Button)sender;
        var menu = new ContextMenu { PlacementTarget = button };
        var edit = new MenuItem { Header = "编辑" };
        edit.Click += (_, _) => EditSlot(slot);
        var delete = new MenuItem { Header = "删除" };
        delete.Click += (_, _) => DeleteSlot(slot);
        menu.Items.Add(edit);
        menu.Items.Add(delete);
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var slot = new NoteSlot { Order = _state.Slots.Count };
        var editor = new NoteEditorWindow(slot) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() != true || (!slot.HasContent && string.IsNullOrWhiteSpace(slot.Title))) return;
        _state.Slots.Add(slot);
        SaveAndRefresh();
    }

    private void EditSlot(NoteSlot slot)
    {
        var editor = new NoteEditorWindow(slot) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() == true) SaveAndRefresh();
    }

    private void DeleteSlot(NoteSlot slot)
    {
        _state.Slots.Remove(slot);
        for (var index = 0; index < _state.Slots.Count; index++) _state.Slots[index].Order = index;
        SaveAndRefresh();
    }

    private void SaveAndRefresh()
    {
        NotesStore.Save(_state);
        Refresh();
    }

    private void Slot_Click(object sender, RoutedEventArgs e)
    {
        if (DateTime.UtcNow < _ignoreClicksUntil) { e.Handled = true; return; }
        var slot = (NoteSlot)((Button)sender).Tag;
        if (!slot.HasContent) return;
        NoteViewerLauncher.Open(Window.GetWindow(this), slot, _state, () => NotesStore.Save(_state));
    }

    private void Slot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _draggedSlot = (NoteSlot)((Button)sender).Tag;
    }

    private void Slot_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedSlot is null) return;
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(point.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _ignoreClicksUntil = DateTime.UtcNow.AddMilliseconds(350);
        var data = new DataObject("VibecoreHub.Note", _draggedSlot.Id);
        try { DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move); }
        finally { _draggedSlot = null; ClearInsertionLine(); }
    }

    private void Slot_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("VibecoreHub.Note") || _draggedSlot is null) return;
        var button = (Button)sender;
        var target = button.Tag as NoteSlot;
        if (ReferenceEquals(target, _draggedSlot)) { ClearInsertionLine(); return; }
        var vertical = SettingsStore.Load().NoteColumns > 1;
        _insertAfter = vertical
            ? e.GetPosition(button).X > button.ActualWidth / 2
            : e.GetPosition(button).Y > button.ActualHeight / 2;
        ShowInsertionLine(button, _insertAfter, vertical);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Add_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("VibecoreHub.Note") || _draggedSlot is null) return;
        _insertAfter = false;
        ShowInsertionLine((Button)sender, false, SettingsStore.Load().NoteColumns > 1);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Slot_Drop(object sender, DragEventArgs e)
    {
        if (_draggedSlot is null) return;
        var target = ((Button)sender).Tag as NoteSlot;
        if (ReferenceEquals(target, _draggedSlot)) return;
        var ordered = _state.Slots.OrderBy(slot => slot.Order).ToList();
        ordered.Remove(_draggedSlot);
        var index = target is null ? ordered.Count : ordered.IndexOf(target) + (_insertAfter ? 1 : 0);
        ordered.Insert(Math.Clamp(index, 0, ordered.Count), _draggedSlot);
        _state.Slots = ordered;
        for (var position = 0; position < ordered.Count; position++) ordered[position].Order = position;
        ClearInsertionLine();
        SaveAndRefresh();
        e.Handled = true;
    }

    private void ShowInsertionLine(UIElement target, bool after, bool vertical)
    {
        ClearInsertionLine();
        var layer = AdornerLayer.GetAdornerLayer(target);
        if (layer is null) return;
        _insertionTarget = target;
        _insertionLine = new InsertionLineAdorner(target, vertical, after);
        layer.Add(_insertionLine);
    }

    private void ClearInsertionLine()
    {
        if (_insertionLine is not null && _insertionTarget is not null)
            AdornerLayer.GetAdornerLayer(_insertionTarget)?.Remove(_insertionLine);
        _insertionLine = null;
        _insertionTarget = null;
    }
}
