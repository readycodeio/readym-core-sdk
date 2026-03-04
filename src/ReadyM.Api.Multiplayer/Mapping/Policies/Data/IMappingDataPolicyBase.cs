using System;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Data;

public interface IMappingDataPolicyBase
{
    Type ContextType { get; }
}