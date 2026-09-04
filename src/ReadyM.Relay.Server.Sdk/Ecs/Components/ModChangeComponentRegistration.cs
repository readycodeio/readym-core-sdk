using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

/// <summary>
/// A networked component carries a generated nested <c>ChangeComponent</c> that records which of its fields
/// changed and when. It is a component in its own right, so it has to be declared like one, but a mod author
/// should not have to declare it: it exists because the component is networked.
/// <para>
/// So it is derived here, from every networked component the registry sees. As a filter, which means it does
/// not matter whether this goes in before or after a mod declares its components: a filter is replayed over
/// everything already declared and receives everything declared later.
/// </para>
/// <para>
/// The archetype side does the same thing for the same reason, see
/// <see cref="ModChangeComponentArchetypeRegistration"/>.
/// </para>
/// </summary>
internal sealed class ModChangeComponentRegistration(ServerSideSettings serverSide)
{
    private sealed class Filter : IModComponentRegistryCallback
    {
        public void AcceptComponent<T>(ModComponentRegistry registry) where T : struct
        {
            // Only a networked component has one. A local component is just data and nothing tracks changes
            // to it, which is also why declaring one does not reach this branch.
            if (default(T) is INetworkedComponent netComp)
            {
                registry.RegisterLocalComponent(netComp.GetChangeComponent());
            }
        }
    }

    public void Register(ModComponentRegistry registry)
    {
        if (serverSide.IsServerSide)
        {
            registry.RegisterFilter(new Filter());
        }
    }
}
