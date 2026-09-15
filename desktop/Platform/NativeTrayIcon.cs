using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace VibecoreHub.Desktop.Platform;

internal sealed class NativeTrayIcon : IDisposable
{
    private const int CallbackMessage = 0x8001;
    private const int WmLeftButtonUp = 0x0202;
    private const int WmRightButtonUp = 0x0205;
    private const uint NimAdd = 0;
    private const uint NimDelete = 2;
    private const uint NifMessage = 1;
    private const uint NifIcon = 2;
    private const uint NifTip = 4;
    private readonly HwndSource _source;
    private readonly Action _show;
    private readonly ContextMenu _menu;
    private readonly IntPtr _iconHandle;
    private NotifyIconData _data;

    public NativeTrayIcon(Window window, Action show, Action exit)
    {
        _show = show;
        _source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
        _source.AddHook(WndProc);
        var largeIcons = new IntPtr[1];
        var smallIcons = new IntPtr[1];
        ExtractIconEx(Environment.ProcessPath!, 0, largeIcons, smallIcons, 1);
        _iconHandle = smallIcons[0] != IntPtr.Zero ? smallIcons[0] : largeIcons[0];
        if (largeIcons[0] != IntPtr.Zero && largeIcons[0] != _iconHandle) DestroyIcon(largeIcons[0]);
        _data = new NotifyIconData
        {
            Size = Marshal.SizeOf<NotifyIconData>(), WindowHandle = _source.Handle, Id = 1,
            Flags = NifMessage | NifIcon | NifTip, CallbackMessage = CallbackMessage,
            IconHandle = _iconHandle, Tip = "Vibecore Hub"
        };
        ShellNotifyIcon(NimAdd, ref _data);
        _menu = new ContextMenu();
        var open = new MenuItem { Header = "打开 Vibecore Hub" }; open.Click += (_, _) => show();
        var quit = new MenuItem { Header = "退出" }; quit.Click += (_, _) => exit();
        _menu.Items.Add(open); _menu.Items.Add(new Separator()); _menu.Items.Add(quit);
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != CallbackMessage) return IntPtr.Zero;
        var mouseMessage = unchecked((int)(long)lParam) & 0xFFFF;
        if (mouseMessage == WmLeftButtonUp) _show();
        else if (mouseMessage == WmRightButtonUp)
        {
            _menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            _menu.IsOpen = true;
        }
        handled = true;
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        ShellNotifyIcon(NimDelete, ref _data);
        _source.RemoveHook(WndProc);
        if (_iconHandle != IntPtr.Zero) DestroyIcon(_iconHandle);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int Size;
        public IntPtr WindowHandle;
        public uint Id;
        public uint Flags;
        public int CallbackMessage;
        public IntPtr IconHandle;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint TimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid Guid;
        public IntPtr BalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellNotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(string file, int index, IntPtr[] largeIcons, IntPtr[] smallIcons, uint iconCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
