using ReadyM.Api.DI;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
#if NET
using ReadyM.SDK.Chunks;
using ReadyM.SDK.Client.Chunks;
#endif

namespace ReadyM.SDK.Client;

public static class DependencyInjectionExtensions
{
    public static void RegisterReadyMSdk(this IDependencyContainer container)
    {
        container.RegisterSingleton<IEntities, ClientEntities>();
        container.RegisterSingleton<IEntityApi, ClientEntityApi>();
#if NET
        container.RegisterSingleton<IChunkSource, ClientChunkSource>();
#endif
    }
}