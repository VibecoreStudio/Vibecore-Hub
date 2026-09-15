using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VibecoreHub.Desktop.Models;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

public partial class ShortcutEditorWindow : Window
{
    private readonly HubItem? _original;
    public HubItem? Result { get; private set; }

    public ShortcutEditorWindow(HubItem? item)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        _original = item;
        Heading.Text = item is null ? "添加快捷项" : "编辑快捷项";
        TypeBox.SelectedIndex = item?.Type switch { "file" => 1, "group" => 2, "text" => 3, "folder" => 4, _ => 0 };
        NameBox.Text = item?.Name ?? "";
        TargetBox.Text = item is null ? "" : ShortcutTarget.Normalize(item.Target, item.Type);
        if (item?.Type == "group") TypeBox.IsEnabled = false;
    }

    private string SelectedType => ((ComboBoxItem)TypeBox.SelectedItem).Tag.ToString()!;

    private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TargetLabel is null) return;
        TargetLabel.Text = SelectedType == "text" ? "文本内容" : "路径";
        TargetPanel.Visibility = SelectedType == "group" ? Visibility.Collapsed : Visibility.Visible;
        BrowseButton.Visibility = SelectedType is "text" or "group" ? Visibility.Collapsed : Visibility.Visible;
        Height = SelectedType == "group" ? 270 : 390;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedType == "folder")
        {
            var dialog = new OpenFolderDialog { Title = "选择文件夹", Multiselect = false };
            if (dialog.ShowDialog(this) == true) TargetBox.Text = dialog.FolderName;
            return;
        }
        var picker = new OpenFileDialog
        {
            Title = SelectedType == "application" ? "选择应用程序" : "选择文件",
            Filter = SelectedType == "application" ? "应用程序 (*.exe)|*.exe|所有文件 (*.*)|*.*" : "所有文件 (*.*)|*.*"
        };
        if (picker.ShowDialog() == true)
        {
            TargetBox.Text = picker.FileName;
            if (string.IsNullOrWhiteSpace(NameBox.Text)) NameBox.Text = System.IO.Path.GetFileNameWithoutExtension(picker.FileName);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var target = SelectedType == "group" ? "" : ShortcutTarget.Normalize(TargetBox.Text, SelectedType);
        if (string.IsNullOrWhiteSpace(NameBox.Text) || (SelectedType != "group" && string.IsNullOrWhiteSpace(target))) return;
        Result = new HubItem
        {
            Id = _original?.Id ?? Guid.NewGuid().ToString("N"), Type = SelectedType,
            Name = NameBox.Text.Trim(), Target = target,
            Color = _original?.Color ?? SelectedType switch { "file" => "sky", "application" => "violet", "text" => "rose", _ => "amber" },
            Order = _original?.Order ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), CreatedAt = _original?.CreatedAt ?? DateTimeOffset.UtcNow.ToString("O"),
            GroupId = _original?.GroupId,
            CustomIconPath = _original?.CustomIconPath
            , GroupWindowWidth = _original?.GroupWindowWidth ?? 330
            , GroupWindowHeight = _original?.GroupWindowHeight ?? 350
        };
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
}
