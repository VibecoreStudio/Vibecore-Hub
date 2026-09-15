using System.Runtime.InteropServices;
using System.Threading;

namespace VibecoreHub.Desktop.Platform;

internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    public bool IsFirstInstance { get; }

    public SingleInstance()
    {
        _mutex = new Mutex(true, "Local\\VibecoreHub.Desktop.Singleton", out var created);
        IsFirstInstance = created;
    }

    public static void ActivateExistingWindow()
    {
        var handle = FindWindow(null, "Vibecore Hub");
        if (handle == IntPtr.Zero) return;
        ShowWindow(handle, 9);
        SetForegroundWindow(handle);
    }

    public void Dispose()
    {
        if (IsFirstInstance) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindow(string? className, string windowName);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
}
