using System;
using System.Runtime.InteropServices;

namespace Peep.Avalonia.Interop;

public partial class MacOSInterop
{
    [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr sel_registerName(string selector);

    [LibraryImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static partial void objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    public static void SetIgnoresMouseEvents(IntPtr nsWindow, bool ignoresMouseEvents)
    {
        IntPtr setIgnoresMouseEventsSelector = sel_registerName("setIgnoresMouseEvents:");
        objc_msgSend(nsWindow, setIgnoresMouseEventsSelector, ignoresMouseEvents ? 1 : 0);
    }
}
