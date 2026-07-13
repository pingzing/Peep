using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Peep.Avalonia;

#if WINDOWS
public class LaunchOnStartup
{
    private const string StartupEntryName = "Peep.lnk";

    public bool IsLaunchOnStartup()
    {
        return File.Exists(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupEntryName)
        );
    }

    public static void ToggleOnStartup(bool launchOnStartup)
    {
        string? shortcutPath = GetStartupShortcutPath();

        // Four states:
        // - launchOnStartup, and exists: do nothing
        // - !launchOnStartup, and doesn't exist: do nothing
        // - launchOnStartup, and doesn't exist: create
        // - !launchOnStartup, and exists: delete

        if (launchOnStartup && shortcutPath != null)
        {
            return;
        }
        if (!launchOnStartup && shortcutPath == null)
        {
            return;
        }

        if (launchOnStartup) // implies doesn't exist
        {
            string targetPath = Path.Combine(Environment.ProcessPath!);
            IShellLink shortcut = (IShellLink)new ShellLink();
            shortcut.SetDescription("Startup shortcut for Peep");
            shortcut.SetPath(targetPath);

            shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupEntryName);
            IPersistFile file = (IPersistFile)shortcut;
            file.Save(shortcutPath, false);
        }
        else // Implies exists, and shouldEnable == false
        {
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupEntryName));
        }
    }

    private static string? GetStartupShortcutPath()
    {
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string[] startupEntries = Directory.GetFiles(startupFolder);
        string? shortcutPath = startupEntries.FirstOrDefault(x =>
        {
            string fileName = Path.GetFileName(x);
            return fileName == StartupEntryName;
        });

        return shortcutPath;
    }
}

// Infra classes and interfaces for creating shortcuts
[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
internal class ShellLink { }

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("000214F9-0000-0000-C000-000000000046")]
internal interface IShellLink
{
    void GetPath(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
        int cchMaxPath,
        out IntPtr pfd,
        int fFlags
    );
    void GetIDList(out IntPtr ppidl);
    void SetIDList(IntPtr pidl);
    void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
    void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
    void GetHotkey(out short pwHotkey);
    void SetHotkey(short wHotkey);
    void GetShowCmd(out int piShowCmd);
    void SetShowCmd(int iShowCmd);
    void GetIconLocation(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
        int cchIconPath,
        out int piIcon
    );
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
    void Resolve(IntPtr hwnd, int fFlags);
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}
#endif
