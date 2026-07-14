using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Avalonia.Interop;

public class MacOSInterop
{
    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    public static extern IntPtr objc_getClass(string className);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    public static extern IntPtr sel_registerName(string selector);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    public static extern void objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    public static void SetIgnoresMouseEvents(IntPtr nsWindow, bool ignoresMouseEvents)
    {
        IntPtr setIgnoresMouseEventsSelector = sel_registerName("setIgnoresMouseEvents:");
        objc_msgSend(nsWindow, setIgnoresMouseEventsSelector, ignoresMouseEvents ? 1 : 0);
    }
}
