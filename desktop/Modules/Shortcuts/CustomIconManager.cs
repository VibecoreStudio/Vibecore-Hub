using System.IO;
using System.Windows;
using Microsoft.Win32;
using VibecoreHub.Desktop.Models;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

internal static class CustomIconManager
{
    public static bool Choose(HubItem item, Window? owner)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择自定义图标",
            Filter = "图标或图片|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.exe|所有文件|*.*"
        };
        if (dialog.ShowDialog(owner) != true) return false;
        var directory = Path.Combine(DataProfileManager.CurrentDirectory, "custom-icons");
        Directory.CreateDirectory(directory);
        var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
        var destination = Path.Combine(directory, item.Id + extension);
        if (!string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            File.Copy(dialog.FileName, destination, true);
        item.CustomIconPath = Path.GetRelativePath(AppContext.BaseDirectory, destination);
        return true;
    }

    public static void RestoreDefault(HubItem item) => item.CustomIconPath = null;
}
