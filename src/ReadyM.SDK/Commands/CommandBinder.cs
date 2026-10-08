using System;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;

namespace ReadyM.SDK.Commands;

// Puts every declared command into the one registry, once, at startup
public sealed class CommandBinder(CommandRegistry registry, IDependencyContainer container, ILogger logger)
{
    private bool _bound;

    public void BindAll()
    {
        if (_bound)
        {
            throw new InvalidOperationException(
                $"{nameof(BindAll)} has already run. Commands are bound once, at startup.");
        }

        _bound = true;

        var all = CommandProviders.All;

        for (var i = 0; i < all.Count; i++)
        {
            // One provider with a clashing name must not cost every other provider its commands.
            try
            {
                all[i](container).RegisterCommands(registry);
            }
            catch (Exception e)
            {
                logger.LogError(e, "{Provider} declares a command that could not be registered.",
                    CommandProviders.Types[i].FullName);
            }
        }
    }
}
