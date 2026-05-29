using JetBrains.Annotations;
using ReadyM.Api.DI;

namespace ReadyM.Relay.Server.Sdk;

public abstract class ServerModBase
{
    protected IDependencyContainer Services { get; private set; } = null!;

    [UsedImplicitly]
    public void Initialize(IDependencyContainer services)
    {
        Services = services;
        Init();
    }

    protected abstract void Init();
}