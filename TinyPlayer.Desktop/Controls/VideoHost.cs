using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TinyPlayer.Desktop.Controls;

/// <summary>
///     Native P/Invoke Windows panel for WPF
/// </summary>
public class VideoHost : HwndHost
{
    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;

    public new IntPtr Handle { get; private set; }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        Handle = CreateWindowEx(
            0, "static", "",
            WsChild | WsVisible,
            0, 0, 100, 100,
            hwndParent.Handle,
            IntPtr.Zero, IntPtr.Zero, 0);

        return new HandleRef(this, Handle);
    }

    // При ресайзе WPF элемента — двигаем нативное окно под новый размер
    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);
        if (Handle == IntPtr.Zero)
        {
            return;
        }

        var w = (int)info.NewSize.Width;
        var h = (int)info.NewSize.Height;

        if (w > 0 && h > 0)
        {
            MoveWindow(Handle, 0, 0, w, h, true);
        }
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        DestroyWindow(hwnd.Handle);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CreateWindowEx(
        int exStyle, string className, string windowName, int style,
        int x, int y, int width, int height,
        IntPtr parentHandle, IntPtr menu, IntPtr instance, object param);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(
        IntPtr hwnd, int x, int y, int width, int height, bool repaint);
}