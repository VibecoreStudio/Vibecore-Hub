using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

internal static partial class ShortcutTarget
{
    public static string Normalize(string value, string type)
    {
        var result = value.Trim();
        if (type == "text" || result.Length < 2) return result;
        var quoted = (result[0] == '"' && result[^1] == '"')
            || (result[0] == '“' && result[^1] == '”')
            || (result[0] == '‘' && result[^1] == '’');
        return quoted ? result[1..^1].Trim() : result;
    }

    public static string? ExecutablePath(string target)
    {
        var value = target.Trim();
        if (value.StartsWith('"'))
        {
            var closingQuote = value.IndexOf('"', 1);
            if (closingQuote > 1) return value[1..closingQuote];
        }
        var match = ExecutablePattern().Match(value);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    public static bool TryGetSteamAppId(string target, out string appId)
    {
        var match = SteamPattern().Match(target.Trim());
        appId = match.Success ? match.Groups[1].Value : "";
        return match.Success;
    }

    public static void Launch(string target)
    {
        var value = target.Trim();
        var executable = ExecutablePath(value);
        if (executable is null || !File.Exists(executable))
        {
            Process.Start(new ProcessStartInfo(Normalize(value, "application")) { UseShellExecute = true });
            return;
        }

        var executableEnd = value.StartsWith('"') ? value.IndexOf('"', 1) + 1 : executable.Length;
        var arguments = executableEnd > 0 && executableEnd <= value.Length ? value[executableEnd..].Trim() : "";
        Process.Start(new ProcessStartInfo(executable) { Arguments = arguments, UseShellExecute = true });
    }

    [GeneratedRegex(@"^(.+?\.exe)(?:\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex ExecutablePattern();

    [GeneratedRegex(@"^steam://rungameid/(\d+)(?:/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex SteamPattern();
}
