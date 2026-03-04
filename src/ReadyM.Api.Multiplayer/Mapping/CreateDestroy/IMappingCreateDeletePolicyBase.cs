using System;

namespace ReadyM.Api.Multiplayer.Mapping.CreateDestroy;

public interface IMappingCreateDeletePolicyBase
{
    Type GameObjectType { get; }
}