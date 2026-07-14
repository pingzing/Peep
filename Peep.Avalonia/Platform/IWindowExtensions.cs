using Avalonia.Controls;

namespace Peep.Avalonia.Platform;

public interface IWindowExtensions
{
    /// <summary>
    /// Sets the given window to be hit transparent--it will ignore mouse clicks and mouse movements.
    /// </summary>
    void SetHitTransparent(Window window);
}
