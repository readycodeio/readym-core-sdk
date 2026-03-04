using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Data;

public interface IMappingDataPolicyFactory
{
    bool Supports(Type dataType, Type contextType);
    IMappingDataPolicyBase CreatePolicy(ArchetypeId archetypeId, Type dataType, Type contextType);
    IMappingDataPolicy<TContext> CreatePolicy<TContext>(ArchetypeId archetypeId, Type dataType)
        where TContext : struct;
}