using System.Windows;
using System.Windows.Input;

namespace VibecoreHub.Desktop;

public partial class ProfileEditorWindow : Window
{
    public string ProfileName { get; private set; } = "";
    public bool CopyCurrent { get; private set; }

    public ProfileEditorWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        Loaded += (_, _) => { NameBox.SelectAll(); NameBox.Focus(); };
    }

    public void ConfigureForRename(string currentName)
    {
        Title = "重命名电脑配置";
        Heading.Text = "重命名电脑配置";
        NameBox.Text = currentName;
        CopyBox.Visibility = Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) return;
        ProfileName = NameBox.Text.Trim();
        CopyCurrent = CopyBox.IsChecked == true;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}
