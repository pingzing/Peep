using Peep.Shared;
using Peep.Windows.Shared;
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
            SingletonEnforcer.Enforce(() => Current.Shutdown());
            Peep.Windows.Shared.Startup.DisableWPFTabletSupport();

            string executablePath = System.Environment.ProcessPath!;

            _startupHelper = new Startup(
                trayIcon: NetLatest.Properties.Resources.PeepIcon,
                closedPressed: Current.Shutdown,
                hotkeyPressed: (int hotkeyId, ChosenCharacter chosenCharacter) =>
                {
                    if (_peepWindow == null)
                    {
                        _peepWindow = new PeepWindow() { Topmost = true };
                    }

                    _peepWindow.Peep(chosenCharacter);
                },
                executablePath: executablePath
            );
        }

        private void App_Exit(object sender, ExitEventArgs e) => _startupHelper.UnregisterPeepHotkey();
    }
}
