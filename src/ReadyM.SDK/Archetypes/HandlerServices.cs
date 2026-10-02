using System.ComponentModel;
using ReadyM.Api.DI;

namespace ReadyM.SDK.Archetypes;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class HandlerServices
{
    private static IDependencyContainer? _services;

    /// Call this once when the game initializes to wire up the DI for the handlers.
    public static void Use(IDependencyContainer services) => _services = services;

    public static IDependencyContainer Services
        => _services ?? throw new InvalidOperationException(
            "A handler asked for a service before the game registered the SDK with the SDK's own "
            + "container. Call RegisterReadyMSdk on it first.");
}
