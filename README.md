# Peep

A tiny, silly program to allow the user to press a hotkey (currently hardcoded to Ctrl + Alt + B) and
have a small bat pony show up and "Peep!" at you.

Runs a system tray application, and can be closed at any time just closing it from the systray, or just ending Peep.NetLatest.exe or Peep.NetFx.exe.

Can be registered to run on startup via the system tray context menu. Does so by placing a shortcut to the program executable in the Windows Startup folder at
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup.

## Building

Requires .NET 6 and WPF.
Only runs on Windows. (Sorry.)