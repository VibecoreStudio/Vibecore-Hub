using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VibecoreHub.Desktop.Modules.Notes;

public partial class NoteViewerWindow : Window
{
    private readonly NotesState _state;
    private readonly Action _save;

    public NoteViewerWindow(NoteSlot slot, NotesState state, Action save)
    {
        InitializeComponent();
        _state = state;
        _save = save;
        Width = Math.Max(MinWidth, state.ViewerWidth);
        Height = Math.Max(MinHeight, state.ViewerHeight);
        Title = string.IsNullOrWhiteSpace(slot.Title) ? "便笺" : slot.Title;
        TitleText.Text = Title;
        ContentText.Text = slot.Content;
        SourceInitialized += (_, _) => ApplyRoundedCorners();
        Loaded += (_, _) =>
        {
            if (Owner is not null) { Topmost = Owner.Topmost; Owner.LocationChanged += OwnerMoved; }
            AttachToOwner();
        };
        Closed += (_, _) =>
        {
            if (Owner is not null) Owner.LocationChanged -= OwnerMoved;
            _state.ViewerWidth = ActualWidth;
            _state.ViewerHeight = ActualHeight;
            _save();
        };
    }

    private void OwnerMoved(object? sender, EventArgs e) => AttachToOwner();

    private void AttachToOwner()
    {
        if (Owner is null) return;
        Left = Owner.Left - ActualWidth;
        Top = Owner.Top;
    }

    private void ApplyRoundedCorners()
    {
        var preference = 2;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 33, ref preference, sizeof(int));
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
