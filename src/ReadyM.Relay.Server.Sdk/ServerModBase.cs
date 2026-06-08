using JetBrains.Annotations;
using ReadyM.Api.DI;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Relay.Server.Sdk.Ecs.Systems;

namespace ReadyM.Relay.Server.Sdk;

public abstract class ServerModBase
{
    protected IDependencyContainer Services { get; private set; } = null!;

    [UsedImplicitly]
    public void InitializeAot(IComponentRegistry registry)
    {
        RegisterComponents(registry);
    }

    [UsedImplicitly]
    public void Initialize(IDependencyContainer services)
    {
        Services = services;
        Init();
    }

    /// <summary>
    /// Any components defined in the mod must be registered here.
    /// </summary>
    protected abstract void RegisterComponents(IComponentRegistry registry);

    protected abstract void Init();
}