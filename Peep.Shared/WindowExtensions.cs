using System;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;

namespace Peep.Shared
{
    internal static class WindowExtensions
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;

        internal static void SetWindowExTransparent(IntPtr hwnd)
        {
            HWND handle = new HWND(hwnd);
            int extendedStyle = PInvoke.GetWindowLong(handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            PInvoke.SetWindowLong(handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }
    }
}
