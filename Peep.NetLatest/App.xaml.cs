using Peep.Shared;
using System.Windows;

namespace Peep.NetLatest
{
    public partial class App : Application
    {
        private PeepWindow? _peepWindow = null!;
        private Startup _startupHelper = null!;

        public App()
        {
            Startup += App_Startup;
            Exit += App_Exit;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            Shared.Startup.EnforceSingleInstance(this);
            Shared.Startup.DisableWPFTabletSupport();

            _startupHelper = new Startup(
                trayIcon: NetLatest.Properties.Resources.PeepIcon,
                closedPressed: () => Current.Shutdown(),
                hotkeyPressed: (int hotkeyId) =>
                {
                    if (_peepWindow == null)
                    {
                        _peepWindow = new PeepWindow() { Topmost = true };
                    }

                    _peepWindow.Peep();
                }
            );
        }

        private void App_Exit(object sender, ExitEventArgs e) => _startupHelper.UnregisterPeepHotkey();
    }
}
