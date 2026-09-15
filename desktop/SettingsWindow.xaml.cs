using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VibecoreHub.Desktop.Modules;

namespace VibecoreHub.Desktop;

public sealed class ModuleOrderItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
}

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ObservableCollection<ModuleOrderItem> _modules;
    private readonly ObservableCollection<DataProfileInfo> _profiles;
    private readonly string _activeProfileId;
    public bool ProfileChanged { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        _settings = settings;
        _activeProfileId = DataProfileManager.ActiveProfile.Id;
        _profiles = new(DataProfileManager.GetProfiles());
        ProfileBox.ItemsSource = _profiles;
        ProfileBox.SelectedItem = _profiles.First(profile => profile.Id == _activeProfileId);
        _modules = new(settings.ModuleOrder.Select(id => ModuleRegistry.All.First(module => module.Id == id)).Select(module => new ModuleOrderItem { Id = module.Id, Title = module.Title }));
        ModuleList.ItemsSource = _modules;
        ThemeBox.SelectedIndex = settings.Theme switch { "porcelain" => 1, "aurora" => 2, "blush" => 3, _ => 0 };
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelected(1);

    private void MoveSelected(int offset)
    {
        var index = ModuleList.SelectedIndex;
        var target = index + offset;
        if (index < 0 || target < 0 || target >= _modules.Count) return;
        _modules.Move(index, target); ModuleList.SelectedIndex = target;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.Theme = ((ComboBoxItem)ThemeBox.SelectedItem).Tag.ToString()!;
        _settings.ModuleOrder = _modules.Select(module => module.Id).ToList();
        SettingsStore.Save(_settings);
        if (ProfileBox.SelectedItem is DataProfileInfo selected)
            ProfileChanged = DataProfileManager.Switch(selected.Id);
        ThemeService.Apply(SettingsStore.Load().Theme);
        DialogResult = true;
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProfileEditorWindow { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var profile = DataProfileManager.Create(dialog.ProfileName, dialog.CopyCurrent);
        _profiles.Add(profile);
        ProfileBox.SelectedItem = profile;
    }

    private void RenameProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileBox.SelectedItem is not DataProfileInfo profile) return;
        var dialog = new ProfileEditorWindow { Owner = this };
        dialog.ConfigureForRename(profile.Name);
        if (dialog.ShowDialog() != true || !DataProfileManager.Rename(profile.Id, dialog.ProfileName)) return;
        profile.Name = dialog.ProfileName;
        ProfileBox.Items.Refresh();
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "备份 Vibecore Hub",
            FileName = $"Vibecore Hub Backup {DateTime.Now:yyyy-MM-dd}",
            DefaultExt = ".vchbackup",
            Filter = "Vibecore Hub 备份|*.vchbackup"
        };
        if (dialog.ShowDialog(this) != true) return;
        BackupService.Create(dialog.FileName);
        MessageBox.Show(this, "备份完成。", "Vibecore Hub", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "恢复 Vibecore Hub", Filter = "Vibecore Hub 备份|*.vchbackup" };
        if (dialog.ShowDialog(this) != true) return;
        if (MessageBox.Show(this, "恢复会覆盖当前的设置、快捷方式和笔记，继续吗？", "恢复备份", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        BackupService.Restore(dialog.FileName);
        ThemeService.Apply(SettingsStore.Load().Theme);
        DialogResult = true;
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}
