using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using VibecoreHub.Desktop.Models;

namespace VibecoreHub.Desktop.Modules.Shortcuts;

internal static class IconService
{
    private static readonly ConcurrentDictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? GetApplicationIcon(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;
        return Cache.GetOrAdd(target, static value => ResolveIcon(value));
    }

    public static ImageSource? GetItemIcon(HubItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.CustomIconPath))
        {
            var path = Path.IsPathRooted(item.CustomIconPath) ? item.CustomIconPath : Path.Combine(AppContext.BaseDirectory, item.CustomIconPath);
            if (File.Exists(path))
            {
                var key = $"custom|{path}|{File.GetLastWriteTimeUtc(path).Ticks}";
                return Cache.GetOrAdd(key, _ => path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? ExtractLargeIcon(path) : LoadBitmap(path));
            }
        }
        return item.Type == "application" ? GetApplicationIcon(item.Target) : null;
    }

    private static ImageSource? ResolveIcon(string target)
    {
        if (ShortcutTarget.TryGetSteamAppId(target, out var appId)) return LoadSteamArtwork(appId);
        var executable = ShortcutTarget.ExecutablePath(target) ?? ShortcutTarget.Normalize(target, "application");
        return File.Exists(executable) && executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? ExtractLargeIcon(executable)
            : null;
    }

    private static ImageSource? LoadSteamArtwork(string appId)
    {
        try
        {
            var steamPath = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath") as string;
            if (string.IsNullOrWhiteSpace(steamPath)) return null;
            var cacheDirectory = Path.Combine(steamPath, "appcache", "librarycache", appId);
            var candidates = new[]
            {
                Path.Combine(cacheDirectory, "library_600x900.jpg"),
                Path.Combine(cacheDirectory, "header.jpg"),
                Path.Combine(cacheDirectory, "logo.png")
            };
            var artwork = candidates.FirstOrDefault(File.Exists)
                ?? (Directory.Exists(cacheDirectory) ? Directory.EnumerateFiles(cacheDirectory, "*.jpg").FirstOrDefault() : null);
            if (artwork is not null) return LoadBitmap(artwork);
            var steamExecutable = Path.Combine(steamPath, "steam.exe");
            return File.Exists(steamExecutable) ? ExtractLargeIcon(steamExecutable) : null;
        }
        catch { return null; }
    }

    private static ImageSource? LoadBitmap(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 256;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch { return null; }
    }

    private static ImageSource? ExtractLargeIcon(string executable)
    {
        foreach (var size in new[] { 256, 128, 64, 48, 32 })
        {
            var handles = new IntPtr[1];
            var ids = new uint[1];
            try
            {
                if (PrivateExtractIcons(executable, 0, size, size, handles, ids, 1, 0) == 0 || handles[0] == IntPtr.Zero) continue;
                var source = Imaging.CreateBitmapSourceFromHIcon(handles[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            catch { }
            finally { if (handles[0] != IntPtr.Zero) DestroyIcon(handles[0]); }
        }
        return null;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint PrivateExtractIcons(string file, int index, int width, int height, IntPtr[] icons, uint[] iconIds, uint count, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
