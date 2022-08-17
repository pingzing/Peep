using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Peep
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string UniqueAppId = "3f098cbe-d3a1-40f8-a61e-e20e49b9e2c1";
        private const int PeepHotkeyId = 0;

        private Mutex _singleInstanceMutex = null!;
        private TaskbarIcon _taskbarIcon = null!;
        private MessagingSink _messagingSink = null!;
        private PeepWindow? _peepWindow = null!;

        public App()
        {
            this.Startup += App_Startup;
            this.Exit += App_Exit;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            _singleInstanceMutex = new Mutex(true, UniqueAppId, out bool isNewInstance);
            if (!isNewInstance)
            {
                // If someone already owns this mutex, it means the app is already running.
                // Since this is a single-instance app, shut down.
                Current.Shutdown();
            }

            // Disable tablet support. Otherwise, the loaded stylus support takes up around 200KB.
            DisableWPFTabletSupport();

            _taskbarIcon = new TaskbarIcon();
            _taskbarIcon.Icon = Peep.Properties.Resources.PeepIcon;
            _taskbarIcon.ToolTipText = "Peep!";

            // Close button
            var contextMenu = new ContextMenu();
            MenuItem closeMenuItem = new() { Header = "Close", Command = ApplicationCommands.Close, };
            closeMenuItem.CommandBindings.Add(
                new CommandBinding(ApplicationCommands.Close, CloseExecuted, CanExecuteClose)
            );
            contextMenu.Items.Add(closeMenuItem);

            _taskbarIcon.ContextMenu = contextMenu;
            _taskbarIcon.MenuActivation = PopupActivationMode.LeftOrRightClick;

            // Register global hotkey
            _messagingSink = new MessagingSink();
            BOOL hotkeySuccess = PInvoke.RegisterHotKey(
                _messagingSink.WindowHandle,
                PeepHotkeyId,
                HOT_KEY_MODIFIERS.MOD_CONTROL | HOT_KEY_MODIFIERS.MOD_ALT | HOT_KEY_MODIFIERS.MOD_NOREPEAT,
                0x42 // 'B' key
            );

            if (!hotkeySuccess)
            {
                int lastError = Marshal.GetLastPInvokeError();
                Debug.WriteLine($"Hotkey registry failed! Oh no! LastError: {lastError}");
            }
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            BOOL unregisterSuccess = PInvoke.UnregisterHotKey(_messagingSink.WindowHandle, PeepHotkeyId);
            if (!unregisterSuccess)
            {
                Debug.WriteLine("Oh no! Failed to unregister hotkey!");
            }
        }

        public void HotkeyTriggered()
        {
            if (_peepWindow == null)
            {
                _peepWindow = new PeepWindow() { Topmost = true };
            }

            _peepWindow.Peep();
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void CloseExecuted(object sender, ExecutedRoutedEventArgs e) => Current.Shutdown();

        public static void DisableWPFTabletSupport()
        {
            // Get a collection of the tablet devices for this window.
            TabletDeviceCollection devices = Tablet.TabletDevices;

            if (devices.Count > 0)
            {
                // Get the Type of InputManager.
                Type inputManagerType = typeof(InputManager);

                // Call the StylusLogic method on the InputManager.Current instance.
                object? stylusLogic = inputManagerType.InvokeMember(
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
