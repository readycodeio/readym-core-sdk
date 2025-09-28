using System;

namespace ReadyM.Api.Mapping.CreateDestroy;

public interface IMappingCreateDeletePolicyBase
{
    Type GameObjectType { get; }
}