using System;
using System.Diagnostics;
using Avalonia.Controls;
using Peep.Avalonia.Interop;

namespace Peep.Avalonia.Platform
{
    public class MacOSWindowExtensions : IWindowExtensions
    {
        public void SetHitTransparent(Window window)
        {
            if (!OperatingSystem.IsMacOS())
            {
                return;
            }

            nint? handleWrapper = window.TryGetPlatformHandle()?.Handle;
            if (!handleWrapper.HasValue)
            {
                Debug.WriteLine($"TryGetPlatform() handle failed to get a handle.");
                return;
            }

            nint handle = handleWrapper.Value;
            MacOSInterop.SetIgnoresMouseEvents(handle, true);
        }
    }
}
