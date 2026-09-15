using System.Windows;
using VibecoreHub.Desktop.Models;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

internal static class ShortcutGroupWindowLauncher
{
    public static void Open(Window? owner, HubItem group, HubState state, Action save)
    {
        if (owner is null) return;
        var openWindows = owner.OwnedWindows.OfType<ShortcutGroupWindow>().ToList();
        if (SettingsStore.Load().ShortcutGroupOpenMode == "multiple")
        {
            var existing = openWindows.FirstOrDefault(window => window.GroupId == group.Id);
            if (existing is not null)
            {
                if (existing.WindowState == WindowState.Minimized) existing.WindowState = WindowState.Normal;
                existing.Activate();
                return;
            }
        }
        else
        {
            foreach (var window in openWindows) window.Close();
        }

        new ShortcutGroupWindow(group, state, save) { Owner = owner }.Show();
    }
}
