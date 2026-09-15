using System.IO;
using System.IO.Compression;

namespace VibecoreHub.Desktop;

internal static class BackupService
{
    private static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "data");

    public static void Create(string destination)
    {
        if (File.Exists(destination)) File.Delete(destination);
        using var archive = ZipFile.Open(destination, ZipArchiveMode.Create);
        if (!Directory.Exists(DataDirectory)) return;
        foreach (var file in Directory.EnumerateFiles(DataDirectory, "*", SearchOption.AllDirectories))
            archive.CreateEntryFromFile(file, Path.GetRelativePath(DataDirectory, file).Replace('\\', '/'), CompressionLevel.Optimal);
    }

    public static void Restore(string source)
    {
        Directory.CreateDirectory(DataDirectory);
        using var archive = ZipFile.OpenRead(source);
        var root = Path.GetFullPath(DataDirectory) + Path.DirectorySeparatorChar;
        foreach (var entry in archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)))
        {
            var target = Path.GetFullPath(Path.Combine(DataDirectory, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            var temporary = target + ".restore.tmp";
            entry.ExtractToFile(temporary, true);
            File.Move(temporary, target, true);
        }
    }
}
