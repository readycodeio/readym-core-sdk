
using ReadyM.Api.ECS.Worlds;
using ReadyM.Relay.Server.Sdk;
using ReadyM.Relay.Server.Sdk.Ecs.Components;

namespace ReadyM.SDK.Server;

/// What a server-side mod written against the SDK derives from.
public abstract class ServerMod : ServerModBase
{
    protected sealed override void RegisterComponents(IComponentRegistry registry)
    {
        ServerReplication.RegisterAll(registry);

        RegisterOwnComponents(registry);
    }

    protected sealed override void Init()
    {
        ServerArchetypes.ApplyExtensions(Services.Resolve<IArchetypeRegistry>());

        Start();
    }

    /// The mod's own setup: services, systems, handlers. Runs once every shape has been registered.
    protected abstract void Start();

    /// Components registered by hand, for anything not yet described as a shape. Most mods want
    /// nothing here.
    protected virtual void RegisterOwnComponents(IComponentRegistry registry) { }
}
