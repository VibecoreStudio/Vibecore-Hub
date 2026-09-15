using System.Windows;
using System.Windows.Input;

namespace VibecoreHub.Desktop.Modules.Notes;

public partial class NoteEditorWindow : Window
{
    private readonly NoteSlot _slot;

    public NoteEditorWindow(NoteSlot slot)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        _slot = slot;
        TitleBox.Text = slot.Title;
        ContentBox.Text = slot.Content;
        Loaded += (_, _) => ContentBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _slot.Title = TitleBox.Text.Trim();
        _slot.Content = ContentBox.Text;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}
