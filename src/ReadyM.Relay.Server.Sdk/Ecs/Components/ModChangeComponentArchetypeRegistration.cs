using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

internal class ModChangeComponentArchetypeRegistration(ServerSideSettings serverSide) : IArchetypeRegistration
{
    private class Filter : IArchetypeBuilderCallback
    {
        public void AcceptComponentType<T>(ArchetypeBuilder builder) where T : struct, IComponent
        {
            if (default(T) is INetworkedComponent netComp)
            {
                builder.Add(netComp.GetChangeComponent());
            }
        }

        public void AcceptComponentType<T>(ArchetypeBuilder builder, T defaultValue) where T : struct, IComponent
        {
            if (default(T) is INetworkedComponent netComp)
            {
                builder.Add(netComp.GetChangeComponent());
            }
        }

        public void AcceptStrideComponent(ArchetypeBuilder builder, int structIndex, int stride)
        {
            throw new NotSupportedException("Stride components are not supported on the mod side");
        }

        public void AcceptTag<T>(ArchetypeBuilder builder) where T : struct, ITag
        {
            // no-op
        }
    }

    public void Register(IArchetypeRegistry registry)
    {
        if (serverSide.IsServerSide)
        {
            registry.RegisterFilter(new Filter());
        }
    }
}
