using Hardcodet.Wpf.TaskbarNotification;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.Foundation;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Peep
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string _uniqueAppId = "3f098cbe-d3a1-40f8-a61e-e20e49b9e2c1";
        private Mutex _singleInstanceMutex = null!;
        private TaskbarIcon _taskbarIcon = null!;
        private MessagingSink _messagingSink = null!;

        public App()
        {
            this.Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            _singleInstanceMutex = new Mutex(true, _uniqueAppId, out bool isNewInstance);
            if (!isNewInstance)
            {
                // If someone already owns this mutex, it means the app is already running.
                // Since this is a single-instance app, shut down.
                Current.Shutdown();
            }

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
                _messagingSink.MessagingSinkHwnd,
                0,
                HOT_KEY_MODIFIERS.MOD_CONTROL | HOT_KEY_MODIFIERS.MOD_ALT | HOT_KEY_MODIFIERS.MOD_NOREPEAT,
                0x42 // 'B' key
            );

            if (!hotkeySuccess)
            {
                int lastError = Marshal.GetLastPInvokeError();
                Debug.WriteLine($"Hotkey registry failed! Oh no! LastError: {lastError}");
            }
        }

        public void HotkeyTriggered()
        {
            PeepWindow peepWindow = new() { Topmost = true };
            peepWindow.Show();
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

        private void CloseExecuted(object sender, ExecutedRoutedEventArgs e) => Current.Shutdown();
    }
}
