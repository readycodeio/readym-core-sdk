using ReadyM.Relay.Server.Sdk;
using ReadyM.SDK.Archetypes;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;
using ReadyM.SDK.Server.Systems;
using ReadyM.SDK.Systems;

namespace ReadyM.SDK.Server;

/// What a server-side mod written against the SDK derives from.
public abstract class ServerMod : ServerModBase
{
    protected sealed override void RegisterComponents(IComponentRegistry registry)
    {
        ServerReplication.RegisterAll(registry);

        RegisterOwnComponents(registry);

        ServerArchetypes.ApplyExtensions(registry);
    }

    protected sealed override void Init()
    {
        CreateHandlerRegistry.Use(Services);
        SystemRegistry.RegisterAll(Services);
        
        // Init runs once per mod against the one container they share, so the updater says so once.
        // TODO: Wire as standard system somewhere, since this is based on the obsolete ModSystemBase
        ModSystemUpdates.Wire(Services);

        Start();
    }

    /// The mod's own setup: services, systems, handlers. Runs once every shape has been registered.
    protected abstract void Start();

    /// Components registered by hand, for anything not yet described as a shape. Most mods want
    /// nothing here.
    protected virtual void RegisterOwnComponents(IComponentRegistry registry) { }
}