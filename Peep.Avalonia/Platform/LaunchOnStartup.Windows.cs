#if WINDOWS
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.UI.Shell;

namespace Peep.Avalonia;

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
            IShellLink shortcut = ShellLink.CreateInstance<IShellLink>();
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

[GeneratedComInterface]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("000214F9-0000-0000-C000-000000000046")]
internal partial interface IShellLink
{
    void GetPath([MarshalAs(UnmanagedType.LPWStr)] out string pszFile, int cch, nint pfd, uint fFlags);
    void GetIDList(out nint ppidl);
    void SetIDList(nint pidl);
    void GetDescription([MarshalAs(UnmanagedType.LPWStr)] out string pszName, int cch);
    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] out string pszDir, int cch);
    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
    void GetArguments([MarshalAs(UnmanagedType.LPWStr)] out string pszArgs, int cch);
    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
    void GetHotkey(out short pwHotkey);
    void SetHotkey(short wHotkey);
    void GetShowCmd(out int piShowCmd);
    void SetShowCmd(int iShowCmd);
    void GetIconLocation([MarshalAs(UnmanagedType.LPWStr)] out string pszIconPath, int cch, out int piIcon);
    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
    void Resolve(nint hwnd, uint fFlags);
    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

[GeneratedComInterface]
[Guid("0000010b-0000-0000-C000-000000000046")]
internal partial interface IPersistFile
{
    void GetClassID(out Guid pClassID);
    void IsDirty();
    void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
    void Save(
        [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
        [MarshalAs(UnmanagedType.VariantBool)] bool fRemember
    );
    void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
    void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
}
#endif
