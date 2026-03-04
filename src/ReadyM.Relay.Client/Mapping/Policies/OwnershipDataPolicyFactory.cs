using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using ReadyM.Relay.Client.State;

namespace ReadyM.Relay.Client.Mapping.Policies;

public class OwnershipDataPolicyFactory(ClientOwnershipManager ownership) : IMappingDataPolicyFactory
{
    public bool Supports(Type dataType, Type contextType)
        => contextType == typeof(Entity) && typeof(IOwnershipManaged).IsAssignableFrom(dataType);

    public IMappingDataPolicyBase CreatePolicy(ArchetypeId archetypeId, Type dataType, Type contextType)
    {
        Debug.Assert(contextType == typeof(Entity));
        return new OwnershipDataPolicy(ownership);
    }

    public IMappingDataPolicy<TContext> CreatePolicy<TContext>(ArchetypeId archetypeId, Type dataType) where TContext : struct
        => (IMappingDataPolicy<TContext>)CreatePolicy(archetypeId, dataType, typeof(TContext));
}