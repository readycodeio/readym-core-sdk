using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Archetypes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Client.Events;
using ReadyM.SDK.Client.Mapping;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Events;
using ReadyM.SDK.Services;
#if NET
using ReadyM.SDK.Chunks;
using ReadyM.SDK.Client.Chunks;
#endif

namespace ReadyM.SDK.Client;

internal static class DependencyInjectionExtensions
{
    /// <summary>
    /// Register data mappings declared in all loaded mods.
    /// A game calls this once its mods are loaded and before anything reaches a sync point.
    /// </summary>
    /// <returns>How many sets of mappings were collected, which a game can log.</returns>
    /// <remarks>
    /// Collected once, so every mod has to have registered its own by the time this runs. A mod
    /// entered through [ModEntry] registers them in Init, which is later than it used to be.
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

    /// <summary>Puts what each shape's [Tag]s declared on the archetype the game registered for it.</summary>
    /// <returns>How many shapes had any tags.</returns>
    public static int ApplyArchetypeTags(this IDependencyContainer container)
        => container.Resolve<IEntityApi>() is ClientEntityApi client ? client.ApplyDeclaredTags() : 0;

    public static void ApplyArchetypeExtensions(this IDependencyContainer container)
        => new ArchetypeExtensionRegistration(
            container.Resolve<DefaultWorldArchetypeRegistration>(),
            container.Resolve<DefaultAreaArchetypeRegistration>(),
            container.Resolve<DefaultPlayerArchetypeRegistration>(),
            container.Resolve<DefaultCellArchetypeRegistration>(),
            container.ResolveAll<IArchetypeShapeBindings>(),
            container.Resolve<ILogger>()
        ).Register(container.Resolve<Store>());

    public static void RegisterModDeclarations(this IDependencyContainer container)
    {
        ServiceRegistry.RegisterAll(container);
        RpcHandlerRegistry.RegisterAll(container, RpcSide.Client);

        var networkedEvents = container.Resolve<NetworkedEventRegistry>();
        networkedEvents.DeclareGenerated(AppDomain.CurrentDomain.GetAssemblies());
        networkedEvents.RegisterAll(container, RpcSide.Client, new ClientNetworkedEventCoordinatorRegistration());
    }

    public static void RegisterReadyMSdk(this IDependencyContainer container)
    {
        CreateHandlerRegistry.Use(container);

        container.RegisterSingleton<IEntities, ClientEntities>();
        container.RegisterSingleton<IEntityApi, ClientEntityApi>();
        container.RegisterSingleton<RpcEntities>();
        container.RegisterSingleton<NetworkedEventRegistry>();
        container.RegisterSingleton<INetworkedEventEntityReader, NetworkedEventEntityReader>();
        container.RegisterSingleton<PredictionDeltaHooks>();
        container.RegisterSingleton<GuessedValueRegistry>();
        container.RegisterSingleton<PredictedRaiseRegistry>();
        container.RegisterSingleton<INetworkedEvents, ClientNetworkedEvents>();
#if NET
        container.RegisterSingleton<IChunkSource, ClientChunkSource>();
#endif
    }
}