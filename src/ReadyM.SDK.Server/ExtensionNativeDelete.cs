using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server;

internal static class ExtensionNativeDelete
{
    private static IEntityApi? _api;

    public static void Use(IEntityApi api) => _api = api;

    /// Called for every entity the host is about to delete, whoever asked for it.
    public static void Run(RawEntity entity)
    {
        if (!DeleteHandlerRegistry.Any || _api is null)
            return;

        DeleteHandlerRegistry.RunAll(new EntityHandle(entity, _api));
    }
}
