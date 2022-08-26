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
using Microsoft.Win32;

namespace Peep.Shared
{
    public class Startup
    {
        private const int PeepHotkeyId = 0;
        private const string UniqueAppId = "3f098cbe-d3a1-40f8-a61e-e20e49b9e2c1";

        private Action _closedPressed;
        private Action<int> _hotkeyPressed;

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

        public Startup(Icon trayIcon, Action closedPressed, Action<int> hotkeyPressed)
        {
            _closedPressed = closedPressed;
            _hotkeyPressed = hotkeyPressed;

            // Setup tray icon
            _taskbarIcon = new TaskbarIcon();
            _taskbarIcon.Icon = trayIcon;
            _taskbarIcon.ToolTipText = "Peep!";

            // Close button
            var contextMenu = new ContextMenu();
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

        public void UnregisterPeepHotkey()
        {
            _messagingSink.HotkeyPressed -= MessagingSink_HotkeyPressed;
            BOOL unregisterSuccess = PInvoke.UnregisterHotKey(_messagingSink.WindowHandle, PeepHotkeyId);
            if (!unregisterSuccess)
            {
                Debug.WriteLine("Oh no! Failed to unregister hotkey!");
            }
        }

        private void MessagingSink_HotkeyPressed(object sender, EventArgs e)
        {
            _hotkeyPressed(PeepHotkeyId);
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void CloseExecuted(object sender, ExecutedRoutedEventArgs e) => _closedPressed();

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
