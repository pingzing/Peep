using System.Windows.Controls;
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

        /// <summary>
        /// Command for picking a character to peep (or nyon) at you.
        /// </summary>
        public static readonly RoutedUICommand CharacterChosen = new RoutedUICommand(
            "Character Chosen",
            nameof(CharacterChosen),
            typeof(Startup)
        );
    }

    public class ChosenCharacterCommandArgs
    {
        public ChosenCharacter ChosenCharacter { get; set; }
        public MenuItem CharacterSubmenu { get; set; }
    }
}
