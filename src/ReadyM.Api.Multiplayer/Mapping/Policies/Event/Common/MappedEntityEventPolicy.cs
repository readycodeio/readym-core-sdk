using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Events;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event.Common;

public readonly struct MappedEntityEventPolicy(IMappingEventPolicy<Entity> dataPolicy)
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

    public bool CanGameEventRunLocally(Entity? tamerEntity, out EventSource source)
    {
        if (!tamerEntity.HasValue)
        {
            source = default;
            return false;
        }

        return dataPolicy.CanGameEventRunLocally(tamerEntity.Value, out source);
    }
}