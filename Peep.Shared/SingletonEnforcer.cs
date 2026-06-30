using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Peep.Shared;

public static class SingletonEnforcer
{
    private const string UniqueAppId = "3f098cbe-d3a1-40f8-a61e-e20e49b9e2c1";
    private static Mutex? _singletonMutex = null;

    /// <summary>
    /// Enforces Signleton behavior for the calling app. If an app is not able to acquire
    /// a shared mutex, the passed <paramref name="shutdownCallback"/> will be called.
    /// </summary>
    /// <param name="shutdownCallback"></param>
    public static void Enforce(Action shutdownCallback)
    {
        _singletonMutex = new Mutex(true, UniqueAppId, out bool isNewInstance);
        if (!isNewInstance)
        {
            // If someone already owns this mutex, it means the app is already running.
            // Since this is a single-instance app, shut down.
            shutdownCallback();
        }
    }
}
