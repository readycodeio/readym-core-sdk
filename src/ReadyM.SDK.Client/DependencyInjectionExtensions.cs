using ReadyM.Api.DI;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Archetypes;
using ReadyM.SDK.Services;
using ReadyM.SDK.Client.Mapping;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using ReadyM.Api.Multiplayer.RPC;
#if NET
using ReadyM.SDK.Chunks;
using ReadyM.SDK.Client.Chunks;
#endif

namespace ReadyM.SDK.Client;

public static class DependencyInjectionExtensions
{
    /// Register data mappings declared in all loaded mods.
    /// A game calls this once its mods are loaded and before anything reaches a sync point.
    /// <returns>How many sets of mappings were collected, which a game can log.</returns>
    /// <remarks>
    /// Collected once, so every mod has to have registered its own by the time this runs. A mod
    /// entered through [ModEntry] registers them in Start, which is later than it used to be.
    /// </remarks>
    public static int ApplyShapeMappings(this IDependencyContainer container)
    {
        var mappings = new ShapeMappingRegistry();
        var collected = 0;

        foreach (var declared in container.ResolveAll<IShapeMappings>())
        {
            declared.Register(mappings);
            collected++;
        }

        SyncExtensions.Use(mappings);

        return collected;
    }

    public static void ApplyArchetypeExtensions(this IDependencyContainer container)
        => new ArchetypeExtensionRegistration(
            container.Resolve<DefaultWorldArchetypeRegistration>(),
            container.Resolve<DefaultAreaArchetypeRegistration>(),
            container.Resolve<DefaultPlayerArchetypeRegistration>(),
            container.Resolve<DefaultCellArchetypeRegistration>(),
            container.ResolveAll<IArchetypeShapeBindings>(),
            container.Resolve<ILogger>()
        ).Register(container.Resolve<Store>());

    public static void RegisterReadyMSdk(this IDependencyContainer container)
    {
        CreateHandlerRegistry.Use(container);
        ServiceRegistry.RegisterAll(container);
        RpcHandlerRegistry.RegisterAll(container, RpcSide.Client);

        container.RegisterSingleton<IEntities, ClientEntities>();
        container.RegisterSingleton<IEntityApi, ClientEntityApi>();
#if NET
        container.RegisterSingleton<IChunkSource, ClientChunkSource>();
#endif
    }
}