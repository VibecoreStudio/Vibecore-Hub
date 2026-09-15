using System.Windows;

namespace VibecoreHub.Desktop.Modules.Notes;

internal static class NoteViewerLauncher
{
    public static void Open(Window? owner, NoteSlot slot, NotesState state, Action save)
    {
        if (owner is null) return;
        if (SettingsStore.Load().NoteOpenMode != "multiple")
        {
            foreach (var window in owner.OwnedWindows.OfType<NoteViewerWindow>().ToList())
                window.Close();
        }

        new NoteViewerWindow(slot, state, save) { Owner = owner }.Show();
    }
}
