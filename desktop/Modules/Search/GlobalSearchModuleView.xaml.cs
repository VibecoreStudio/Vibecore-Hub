using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using VibecoreHub.Desktop.Models;
using VibecoreHub.Desktop.Modules.Notes;
using VibecoreHub.Desktop.Modules.Shortcuts;
using VibecoreHub.Desktop.Modules.Folders;

namespace VibecoreHub.Desktop.Modules.Search;

public sealed class SearchResult
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Glyph { get; init; }
    public HubItem? Shortcut { get; init; }
    public NoteSlot? Note { get; init; }
    public FolderDirectoryItem? Folder { get; init; }
}

public partial class GlobalSearchModuleView : UserControl
{
    public GlobalSearchModuleView() { InitializeComponent(); Loaded += (_, _) => SearchBox.Focus(); }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (ResultsList is null) return;
        var query = SearchBox.Text.Trim();
        if (query.Length == 0) { ResultsList.ItemsSource = null; HintText.Text = "搜索快捷方式和便笺"; return; }
        var results = new List<SearchResult>();
        foreach (var item in HubStore.Load().Items.Where(item => item.Type != "group" && (item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) || item.Target.Contains(query, StringComparison.CurrentCultureIgnoreCase))))
            results.Add(new SearchResult { Title = item.Name, Subtitle = item.TypeLabel, Glyph = item.Glyph, Shortcut = item });
        foreach (var note in NotesStore.Load().Slots.Where(note => note.HasContent && (note.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) || note.Content.Contains(query, StringComparison.CurrentCultureIgnoreCase))))
            results.Add(new SearchResult { Title = note.DisplayTitle, Subtitle = "便笺", Glyph = "\uE70B", Note = note });
        foreach (var folder in FolderDirectoryStore.Load().Items.Where(folder => folder.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) || folder.Path.Contains(query, StringComparison.CurrentCultureIgnoreCase)))
            results.Add(new SearchResult { Title = folder.Name, Subtitle = "文件夹目录", Glyph = "\uE8B7", Folder = folder });
        ResultsList.ItemsSource = results;
        HintText.Text = results.Count == 0 ? "没有找到结果" : $"找到 {results.Count} 项";
    }

    private void Result_Click(object sender, RoutedEventArgs e)
    {
        var result = (SearchResult)((Button)sender).Tag;
        if (result.Folder is not null)
        {
            try { Process.Start(new ProcessStartInfo(result.Folder.Path) { UseShellExecute = true }); } catch { System.Media.SystemSounds.Exclamation.Play(); }
            return;
        }
        if (result.Note is not null)
        {
            var state = NotesStore.Load();
            var note = state.Slots.FirstOrDefault(slot => slot.Id == result.Note.Id) ?? result.Note;
            NoteViewerLauncher.Open(Window.GetWindow(this), note, state, () => NotesStore.Save(state));
            return;
        }
        if (result.Shortcut is null) return;
        try
        {
            if (result.Shortcut.Type == "text") Clipboard.SetText(result.Shortcut.Target);
            else if (result.Shortcut.Type == "application") ShortcutTarget.Launch(result.Shortcut.Target);
            else Process.Start(new ProcessStartInfo(result.Shortcut.Target) { UseShellExecute = true });
        }
        catch { System.Media.SystemSounds.Exclamation.Play(); }
    }
}
