using System.Windows.Input;

namespace Peep.Shared
{
    public static class CustomCommands
    {
        /// <summary>
        /// Command for toggling the "Launch on Startup" setting.
        /// </summary>
        public static readonly RoutedUICommand ToggleOnStartup = new RoutedUICommand(
            "Toggle Launch on Startup",
            nameof(ToggleOnStartup),
            typeof(Startup)
        );
    }
}
