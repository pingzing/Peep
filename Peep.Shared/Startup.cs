using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace Peep.Shared
{
    public class Startup
    {
        private const int PeepHotkeyId = 0;
        private const string UniqueAppId = "3f098cbe-d3a1-40f8-a61e-e20e49b9e2c1";
        private const string StartupEntryName = "Peep.lnk";

        private readonly string _executablePath;

        private readonly Action _closedPressed;
        private readonly Action<int> _hotkeyPressed;

        private MessagingSink _messagingSink = null;
        private TaskbarIcon _taskbarIcon = null;

        public static void EnforceSingleInstance(Application app)
        {
            _ = new Mutex(true, UniqueAppId, out bool isNewInstance);
            if (!isNewInstance)
            {
                // If someone already owns this mutex, it means the app is already running.
                // Since this is a single-instance app, shut down.
                app.Shutdown();
            }
        }

        public Startup(Icon trayIcon, Action closedPressed, Action<int> hotkeyPressed, string executablePath)
        {
            _closedPressed = closedPressed;
            _hotkeyPressed = hotkeyPressed;
            _executablePath = executablePath;

            // Setup tray icon
            _taskbarIcon = new TaskbarIcon();
            _taskbarIcon.Icon = trayIcon;
            _taskbarIcon.ToolTipText = "Peep!";

            var contextMenu = new ContextMenu();

            MenuItem toggleStartupMenuItem = BuildToggleStartupButton();
            contextMenu.Items.Add(toggleStartupMenuItem);

            // Close button
            MenuItem closeMenuItem = new MenuItem() { Header = "Close", Command = ApplicationCommands.Close, };
            closeMenuItem.CommandBindings.Add(
                new CommandBinding(ApplicationCommands.Close, CloseExecuted, CanExecuteClose)
            );
            contextMenu.Items.Add(closeMenuItem);

            _taskbarIcon.ContextMenu = contextMenu;
            _taskbarIcon.MenuActivation = PopupActivationMode.LeftOrRightClick;

            // Register global hotkey
            _messagingSink = new MessagingSink();
            _messagingSink.HotkeyPressed += MessagingSink_HotkeyPressed;
            BOOL hotkeySuccess = PInvoke.RegisterHotKey(
                _messagingSink.WindowHandle,
                PeepHotkeyId,
                HOT_KEY_MODIFIERS.MOD_CONTROL | HOT_KEY_MODIFIERS.MOD_ALT | HOT_KEY_MODIFIERS.MOD_NOREPEAT,
                0x42 // 'B' key
            );

            if (!hotkeySuccess)
            {
                int lastError = Marshal.GetLastWin32Error();
                Debug.WriteLine($"Hotkey registry failed! Oh no! LastError: {lastError}");
            }
        }

        private MenuItem BuildToggleStartupButton()
        {
            bool isEnabled = GetStartupShortcutPath() != null;

            MenuItem toggleStartup = new MenuItem
            {
                Header = "Launch on startup",
                Command = CustomCommands.ToggleOnStartup,
                IsCheckable = true,
                IsChecked = isEnabled
            };
            toggleStartup.CommandBindings.Add(
                new CommandBinding(CustomCommands.ToggleOnStartup, ToggleOnStartupExecuted, CanExecuteToggleOnStartup)
            );
            return toggleStartup;
        }

        public void UnregisterPeepHotkey()
        {
            _messagingSink.HotkeyPressed -= MessagingSink_HotkeyPressed;
            BOOL unregisterSuccess = PInvoke.UnregisterHotKey(_messagingSink.WindowHandle, PeepHotkeyId);
            if (!unregisterSuccess)
            {
                Debug.WriteLine("Oh no! Failed to unregister hotkey!");
            }
        }

        private void MessagingSink_HotkeyPressed(object sender, int hotkeyId)
        {
            _hotkeyPressed(hotkeyId);
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void CloseExecuted(object sender, ExecutedRoutedEventArgs e) => _closedPressed();

        private void CanExecuteToggleOnStartup(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void ToggleOnStartupExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // todo: if can find entry in startup, remove and untick checkbox
            // else add and tick checkbox
            // sender is MenuItem, and IsChecked has ALREADY been updated by the time we get here.
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (string.IsNullOrEmpty(startupFolder))
            {
                return;
            }

            MenuItem toggleStartup = (sender as MenuItem);
            if (toggleStartup == null)
            {
                return;
            }

            bool shouldEnable = toggleStartup.IsChecked;

            string shortcutPath = GetStartupShortcutPath();

            // Four states:
            // - ShouldEnable, and doesn't exist: create
            // - ShouldEnable, and exists: do nothing
            // - !ShouldEnable, and doesn't exist: do nothing
            // - !ShouldEnable, and exists: delete

            if (shouldEnable && shortcutPath != null)
            {
                return;
            }

            if (!shouldEnable && shortcutPath == null)
            {
                return;
            }

            if (shouldEnable) // ímplies doesn't exist
            {
                string targetPath = Path.Combine(AppContext.BaseDirectory, _executablePath);
                IShellLink shortcut = (IShellLink)new ShellLink();
                shortcut.SetDescription("Startup shortcut for Peep");
                shortcut.SetPath(targetPath);

                shortcutPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    StartupEntryName
                );
                IPersistFile file = (IPersistFile)shortcut;
                file.Save(shortcutPath, false);
            }
            else // implies exists
            {
                File.Delete(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupEntryName)
                );
            }
        }

        private string GetStartupShortcutPath()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string[] startupEntries = Directory.GetFiles(startupFolder);
            string shortcutPath = startupEntries.FirstOrDefault(x =>
            {
                string fileName = Path.GetFileName(x);
                return fileName == StartupEntryName;
            });

            return shortcutPath;
        }

        public static void DisableWPFTabletSupport()
        {
            // Get a collection of the tablet devices for this window.
            TabletDeviceCollection devices = Tablet.TabletDevices;

            if (devices.Count > 0)
            {
                // Get the Type of InputManager.
                Type inputManagerType = typeof(InputManager);

                // Call the StylusLogic method on the InputManager.Current instance.
                object stylusLogic = inputManagerType.InvokeMember(
                    "StylusLogic",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    InputManager.Current,
                    null
                );

                if (stylusLogic != null)
                {
                    //  Get the type of the stylusLogic returned from the call to StylusLogic.
                    Type stylusLogicType = stylusLogic.GetType();

                    // Loop until there are no more devices to remove.
                    while (devices.Count > 0)
                    {
                        // Remove the first tablet device in the devices collection.
                        stylusLogicType.InvokeMember(
                            "OnTabletRemoved",
                            BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                            null,
                            stylusLogic,
                            new object[] { (uint)0 }
                        );
                    }
                }
            }
        }
    }
}
