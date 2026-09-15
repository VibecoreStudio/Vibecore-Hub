using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;

namespace VibecoreHub.Desktop.Modules.Folders;

public partial class FolderDirectoryEditorWindow : Window
{
    private readonly FolderDirectoryItem? _original;
    public FolderDirectoryItem? Result { get; private set; }

    public FolderDirectoryEditorWindow(FolderDirectoryItem? item)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        _original = item;
        Heading.Text = item is null ? "添加文件夹" : "编辑文件夹";
        NameBox.Text = item?.Name ?? "";
        PathBox.Text = item?.Path ?? "";
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "选择文件夹", Multiselect = false };
        if (dialog.ShowDialog(this) != true) return;
        PathBox.Text = dialog.FolderName;
        if (string.IsNullOrWhiteSpace(NameBox.Text)) NameBox.Text = System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(dialog.FolderName));
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(path)) return;
        Result = new FolderDirectoryItem { Id = _original?.Id ?? Guid.NewGuid().ToString("N"), Name = NameBox.Text.Trim(), Path = path, Order = _original?.Order ?? int.MaxValue };
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}
