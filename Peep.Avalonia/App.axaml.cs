using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Peep.Shared;
using SharpHook;
using SharpHook.Providers;

namespace Peep.Avalonia
{
    // TODO: Things that need crossplatform work still
    // - Launch on startup (probably per-platform, Windows is done)
    // - Click-through Windows (definitely per-platform, Windows is done)
    public partial class App : Application
    {
        private IClassicDesktopStyleApplicationLifetime _desktopLifetime = null!;
        private NativeMenu _systemTrayMenu = null!;
        private GlobalHookBase? _hook = null;
        private Task? _hookTask = null;
        private PeepWindow? _peepWindow = null;
        private Settings _settings = null!;

        public PixelPoint LastMousePosition { get; private set; }

        // Launch on startup button
        private NativeMenuItem? _launchOnStartupButton = null;

        // Character context menu buttons
        private NativeMenuItem _ventressButton = null!;
        private NativeMenuItem _kawkawButton = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            // TODO: There's probably a less brittle way to do this. Databinding?
            _systemTrayMenu = TrayIcon.GetIcons(this)![0].Menu!;

            int characterMenuIndex;
            if (OperatingSystem.IsWindows())
            {
                characterMenuIndex = 1;
                _launchOnStartupButton = (NativeMenuItem)_systemTrayMenu.Items[0];
            }
            else
            {
                characterMenuIndex = 0;
            }
            _ventressButton = (NativeMenuItem)((NativeMenuItem)_systemTrayMenu.Items[characterMenuIndex]).Menu.Items[0];
            _kawkawButton = (NativeMenuItem)((NativeMenuItem)_systemTrayMenu.Items[characterMenuIndex]).Menu.Items[1];

            _settings = new Settings(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));

            ChosenCharacter initialChosenCharacter = _settings.ChosenCharacter;
            switch (initialChosenCharacter)
            {
                case ChosenCharacter.Ventress:
                    _ventressButton.IsChecked = true;
                    break;
                case ChosenCharacter.KawKaw:
                    _kawkawButton.IsChecked = true;
                    break;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _desktopLifetime = desktop;
                _desktopLifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                desktop.Startup += Desktop_Startup;
                desktop.Exit += Desktop_Exit;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_Startup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
        {
            SingletonEnforcer.Enforce(() => _desktopLifetime.Shutdown());

            UioHookProvider.Instance.KeyTypedEnabled = false;
            _hook = new EventLoopGlobalHook(SharpHook.Data.GlobalHookType.All);
            _hook.KeyPressed += Hook_KeyPressed;
            _hook.KeyReleased += Hook_KeyReleased;
            _hook.MouseMoved += MouseHook_MouseMoved;
            _hookTask = _hook.RunAsync();
        }

        private bool _isCtrlDown = false;
        private bool _isAltDown = false;

        private void Hook_KeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            if (
                e.Data.KeyCode == SharpHook.Data.KeyCode.VcLeftAlt
                || e.Data.KeyCode == SharpHook.Data.KeyCode.VcRightAlt
            )
            {
                _isAltDown = true;
            }
            if (
                e.Data.KeyCode == SharpHook.Data.KeyCode.VcLeftControl
                || e.Data.KeyCode == SharpHook.Data.KeyCode.VcRightControl
            )
            {
                _isCtrlDown = true;
            }
            if (e.Data.KeyCode == SharpHook.Data.KeyCode.VcB && _isCtrlDown && _isAltDown)
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (_peepWindow == null)
                        {
                            _peepWindow = new PeepWindow();
                        }

                        _ = _peepWindow.Peep(_settings.ChosenCharacter, LastMousePosition);
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private void Hook_KeyReleased(object? sender, KeyboardHookEventArgs e)
        {
            if (
                e.Data.KeyCode == SharpHook.Data.KeyCode.VcLeftAlt
                || e.Data.KeyCode == SharpHook.Data.KeyCode.VcRightAlt
            )
            {
                _isAltDown = false;
            }
            if (
                e.Data.KeyCode == SharpHook.Data.KeyCode.VcLeftControl
                || e.Data.KeyCode == SharpHook.Data.KeyCode.VcRightControl
            )
            {
                _isCtrlDown = false;
            }
        }

        private void MouseHook_MouseMoved(object? sender, MouseHookEventArgs e)
        {
            LastMousePosition = new(e.Data.X, e.Data.Y);
        }

        private async void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            // Unhook the hooks.
            _hook?.Dispose();
            if (_hookTask != null)
            {
                await _hookTask;
            }
        }

        private void LaunchOnStartup_Click(object? sender, System.EventArgs e)
        {
#if WINDOWS
            NativeMenuItem menuItem = (NativeMenuItem)sender!;
            LaunchOnStartup.ToggleOnStartup(menuItem.IsChecked);
#endif
        }

        private void CharacterVentress_Click(object? sender, System.EventArgs e)
        {
            _settings.ChosenCharacter = ChosenCharacter.Ventress;
        }

        private void CharacterKawKaw_Click(object? sender, System.EventArgs e)
        {
            _settings.ChosenCharacter = ChosenCharacter.KawKaw;
        }

        private void Close_Click(object? sender, System.EventArgs e)
        {
            _desktopLifetime.Shutdown();
        }
    }
}
