using Peep.Shared;
using System;
using System.Windows;

namespace Peep.NetFx
{
    public partial class App : Application
    {
        private PeepWindow _peepWindow = null;
        private Startup _startupHelper = null;

        public App()
        {
            Startup += App_Startup;
            Exit += App_Exit;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            Shared.Startup.EnforceSingleInstance(this);
            Shared.Startup.DisableWPFTabletSupport();

            _startupHelper = new Startup();
            _startupHelper.TrayClosedPressed += ClosePressed;
            _startupHelper.SetupTaskbarIcon(NetFx.Properties.Resources.PeepIcon);
            _startupHelper.RegisterPeepHotkey();
            _startupHelper.HotkeyPressed += StartupHelper_HotkeyPressed;
        }

        private void StartupHelper_HotkeyPressed(object sender, EventArgs e)
        {
            if (_peepWindow == null)
            {
                _peepWindow = new PeepWindow() { Topmost = true };
            }

            _peepWindow.Peep();
        }

        private void ClosePressed(object sender, EventArgs e) => Current.Shutdown();

        private void App_Exit(object sender, ExitEventArgs e) => _startupHelper.UnregisterPeepHotkey();
    }
}
