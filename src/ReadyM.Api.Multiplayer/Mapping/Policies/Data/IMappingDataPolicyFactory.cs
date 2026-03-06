using System;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Data;

public interface IMappingDataPolicyFactory
{
    bool Supports(Type dataType, Type contextType);
    IMappingDataPolicyBase CreatePolicy(Type componentType, Type contextType);
    IMappingDataPolicy<TContext> CreatePolicy<TContext>(Type componentType);
}