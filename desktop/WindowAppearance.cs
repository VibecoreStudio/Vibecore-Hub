using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VibecoreHub.Desktop;

internal static class WindowAppearance
{
    public static void ApplyLargeCorners(Window window)
    {
        var preference = 2;
        DwmSetWindowAttribute(new WindowInteropHelper(window).Handle, 33, ref preference, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
