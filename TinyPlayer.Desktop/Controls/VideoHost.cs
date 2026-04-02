using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TinyPlayer.Desktop.Controls;

public class VideoHost : HwndHost
{
    private IntPtr _hwndHost;

    public IntPtr Handle => _hwndHost;

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        _hwndHost = CreateWindowEx(
            0,
            "static",
            "",
            WS_CHILD | WS_VISIBLE,
            0, 0, 100, 100,
            hwndParent.Handle,
            IntPtr.Zero,
            IntPtr.Zero,
            0);

        return new HandleRef(this, _hwndHost);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        DestroyWindow(hwnd.Handle);
    }

    // --- Win32 ---

    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CreateWindowEx(
        int exStyle,
        string className,
        string windowName,
        int style,
        int x, int y,
        int width, int height,
        IntPtr parentHandle,
        IntPtr menu,
        IntPtr instance,
        object param);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);
}

