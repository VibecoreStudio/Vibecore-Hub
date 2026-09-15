using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

public partial class ShortcutSettingsWindow : Window
{
    private readonly AppSettings _settings;

    public ShortcutSettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        _settings = settings;
        IconSizeSlider.Value = settings.ShortcutIconSize;
        ColumnsSlider.Value = settings.ShortcutColumns;
        GapSlider.Value = settings.ShortcutGap;
        GroupOpenModeBox.SelectedIndex = settings.ShortcutGroupOpenMode == "multiple" ? 1 : 0;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShortcutIconSize = (int)IconSizeSlider.Value;
        _settings.ShortcutColumns = (int)ColumnsSlider.Value;
        _settings.ShortcutGap = (int)GapSlider.Value;
        _settings.ShortcutGroupOpenMode = (GroupOpenModeBox.SelectedItem as ComboBoxItem)?.Tag as string == "multiple" ? "multiple" : "single";
        SettingsStore.Save(_settings);
        DialogResult = true;
    }

    private void IconSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (IconSizeValue is not null) IconSizeValue.Text = $"{(int)e.NewValue}px"; }
    private void ColumnsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (ColumnsValue is not null) ColumnsValue.Text = ((int)e.NewValue).ToString(); }
    private void GapSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (GapValue is not null) GapValue.Text = $"{(int)e.NewValue}px"; }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}
