using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace VibecoreHub.Desktop.Modules.Notes;

public partial class NotesSettingsWindow : Window
{
    private readonly AppSettings _settings;

    public NotesSettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        _settings = settings;
        ColumnsSlider.Value = settings.NoteColumns;
        HeightSlider.Value = settings.NoteRowHeight;
        OpenModeBox.SelectedIndex = settings.NoteOpenMode == "multiple" ? 1 : 0;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.NoteRowHeight = (int)HeightSlider.Value;
        _settings.NoteColumns = (int)ColumnsSlider.Value;
        _settings.NoteOpenMode = (OpenModeBox.SelectedItem as ComboBoxItem)?.Tag as string == "multiple" ? "multiple" : "single";
        SettingsStore.Save(_settings);
        DialogResult = true;
    }

    private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (HeightValue is not null) HeightValue.Text = $"{(int)e.NewValue}px"; }
    private void ColumnsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (ColumnsValue is not null) ColumnsValue.Text = $"{(int)e.NewValue} 个"; }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}
