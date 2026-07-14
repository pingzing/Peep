#if WINDOWS
using Avalonia.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Peep.Avalonia.Platform;

public class WindowsWindowExtensions : IWindowExtensions
{
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    public void SetHitTransparent(Window window)
    {
        nint? handle = window.TryGetPlatformHandle()?.Handle;
        if (!handle.HasValue)
        {
            Debug.WriteLine($"TryGetPlatform() handle failed to get a handle.");
            return;
        }
        HWND hwnd = (HWND)handle.Value;

        int currentStyle = PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        if (currentStyle == 0)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.WriteLine($"GetWindowLong() failed with error code 0x{errorCode}.");
            return;
        }

        // Clear LastError because of SetWindowLong quirks
        PInvoke.SetLastError(WIN32_ERROR.ERROR_SUCCESS);
        int result = PInvoke.SetWindowLong(
            hwnd,
            WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
            currentStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT
        );
        if (result == 0)
        {
            int lastError = Marshal.GetLastWin32Error();
            Debug.WriteLine($"SetWindowLong() failed, and returned error code: 0x{lastError:X}");
            return;
        }
    }
}
#endif
