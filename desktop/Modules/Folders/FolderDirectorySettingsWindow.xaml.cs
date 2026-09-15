using System.Windows;
using System.Windows.Input;

namespace VibecoreHub.Desktop.Modules.Folders;

public partial class FolderDirectorySettingsWindow : Window
{
    private readonly AppSettings _settings;
    public FolderDirectorySettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        _settings = settings;
        ColumnsSlider.Value = settings.FolderDirectoryColumns;
        HeightSlider.Value = settings.FolderDirectoryButtonHeight;
    }
    private void Save_Click(object sender, RoutedEventArgs e) { _settings.FolderDirectoryColumns = (int)ColumnsSlider.Value; _settings.FolderDirectoryButtonHeight = (int)HeightSlider.Value; SettingsStore.Save(_settings); DialogResult = true; }
    private void ColumnsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (ColumnsValue is not null) ColumnsValue.Text = ((int)e.NewValue).ToString(); }
    private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (HeightValue is not null) HeightValue.Text = $"{(int)e.NewValue}px"; }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}
