using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using VibecoreHub.Desktop.Models;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

internal static partial class DroppedShortcutFactory
{
    public static HubItem? Create(string path, long order)
    {
        try
        {
            if (Directory.Exists(path)) return NewItem("folder", DirectoryName(path), path, order);
            if (!File.Exists(path)) return null;
            var extension = Path.GetExtension(path);
            var name = Path.GetFileNameWithoutExtension(path);
            if (extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                var resolved = ResolveWindowsShortcut(path);
                if (resolved is null) return NewItem("application", name, path, order);
                var (target, arguments) = resolved.Value;
                var steam = SteamLaunchPattern().Match(arguments);
                if (steam.Success) return NewItem("application", name, $"steam://rungameid/{steam.Groups[1].Value}", order);
                var command = string.IsNullOrWhiteSpace(arguments) ? target : $"\"{target}\" {arguments}";
                return NewItem("application", name, command, order);
            }
            if (extension.Equals(".url", StringComparison.OrdinalIgnoreCase))
            {
                var url = File.ReadLines(path).FirstOrDefault(line => line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))?[4..].Trim();
                return string.IsNullOrWhiteSpace(url) ? null : NewItem("application", name, url, order);
            }
            return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                ? NewItem("application", name, path, order)
                : NewItem("file", name, path, order);
        }
        catch { return null; }
    }

    private static HubItem NewItem(string type, string name, string target, long order) => new()
    {
        Type = type,
        Name = name,
        Target = target,
        Order = order,
        Color = type switch { "file" => "sky", "application" => "violet", _ => "amber" }
    };

    private static string DirectoryName(string path)
    {
        var trimmed = Path.TrimEndingDirectorySeparator(path);
        return Path.GetFileName(trimmed) is { Length: > 0 } name ? name : trimmed;
    }

    private static (string Target, string Arguments)? ResolveWindowsShortcut(string path)
    {
        object? shell = null;
        object? shortcut = null;
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type is null) return null;
            shell = Activator.CreateInstance(type);
            if (shell is null) return null;
            shortcut = shell.GetType().InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, [path]);
            if (shortcut is null) return null;
            var target = shortcut.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, shortcut, null)?.ToString() ?? "";
            var arguments = shortcut.GetType().InvokeMember("Arguments", System.Reflection.BindingFlags.GetProperty, null, shortcut, null)?.ToString() ?? "";
            return string.IsNullOrWhiteSpace(target) ? null : (target, arguments);
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut)) Marshal.FinalReleaseComObject(shortcut);
            if (shell is not null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell);
        }
    }

    [GeneratedRegex(@"(?:^|\s)-applaunch\s+(\d+)(?:\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex SteamLaunchPattern();
}
