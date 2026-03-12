using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event.Common;

internal readonly struct MappedEntityEventPolicy(IMappingEventPolicy<Entity> dataPolicy)
{
    public bool CanGameEventNotifyEcs(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.CanGameEventNotifyEcs(tamerEntity.Value);
    }

    [Obsolete("Is this event needed in the API?")]
    public bool CanEcsInvokeGameEvent(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.CanEcsInvokeGameEvent(tamerEntity.Value);
    }

    public bool CanGameEventRunLocally(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
        {
            return false;
        }

        return dataPolicy.CanGameEventRunLocally(tamerEntity.Value);
    }
}