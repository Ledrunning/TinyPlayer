using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TinyPlayer.Desktop.Controls
{
    /// <summary>
    /// Minimum HwndHost creates a native Win32 window,
    /// the HWND of which is passed to GStreamer VideoOverlay.
    /// </summary>
    public sealed class VideoHwndHost : HwndHost
    {
        private IntPtr _hwnd;

        public event Action<IntPtr> HandleReady;

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            _hwnd = CreateWindowEx(
                0,
                "STATIC",
                "",
                WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
                0, 0, 1, 1,
                hwndParent.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            HandleReady?.Invoke(_hwnd);
            return new HandleRef(this, _hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
        }

        // P/Invoke

        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hwnd);
    }
}
