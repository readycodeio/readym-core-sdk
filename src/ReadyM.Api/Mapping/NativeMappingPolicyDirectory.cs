using System;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping;

internal class NativeMappingPolicyDirectory(
    DataSideChannel sideChannel,
    INativeComponentRegistry registry,
    IMappedEntityManager<IntPtr> entityMapper,
    ILogger logger
) : MappingPolicyDirectory(sideChannel), INativeMappingPolicyDirectory
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ManagedBinding
    {
        public IntPtr ManagedTarget;
    }

    public ManagedBinding GetManagedBinding() => new()
    {
        ManagedTarget = GCHandle.ToIntPtr(GCHandle.Alloc(this))
    };

    public bool CanGameEventNotifyEcs(int eventId)
    {
        var type = registry.GetComponentType(eventId);

        if (type == null)
            throw new ArgumentException($"No component type found for event ID {eventId}");

        var policy = ForEvent<EmptyContext>(type);
        return policy.CanGameEventNotifyEcs(default);
    }

    // TODO: For now, we hard-code IntPtr as context, for IOwnershipBased events
    public bool CanGameEventNotifyEcs(int eventId, IntPtr context)
    {
        var type = registry.GetComponentType(eventId);

        if (type == null)
            throw new ArgumentException($"No component type found for event ID {eventId}");

        if (!entityMapper.IsMapped(context, out var entity))
        {
            logger.LogError("Failed to map entity context {Context} for native Game event with id {EventId} and type {eventType}", context, eventId, type.FullName);
            return false;
        }

        return ForEvent<Entity>(type).CanGameEventNotifyEcs(entity.Value);
    }

    public bool CanEcsInvokeGameEvent(int eventId)
    {
        var type = registry.GetComponentType(eventId);

        if (type == null)
            throw new ArgumentException($"No component type found for event ID {eventId}");

        var policy = ForEvent<EmptyContext>(type);
        return policy.CanEcsInvokeGameEvent(default);
    }

    public bool CanEcsInvokeGameEvent(int eventId, IntPtr context)
    {
        var type = registry.GetComponentType(eventId);

        if (type == null)
            throw new ArgumentException($"No component type found for event ID {eventId}");

        if (!entityMapper.IsMapped(context, out var entity))
        {
            logger.LogError("Failed to map entity context {Context} for native Game event with id {EventId} and type {eventType}", context, eventId, type.FullName);
            return false;
        }

        return ForEvent<Entity>(type).CanEcsInvokeGameEvent(entity.Value);
    }

    public bool CanGameEventRunLocally(int eventId)
    {
        var type = registry.GetComponentType(eventId);

        if (type == null)
            throw new ArgumentException($"No component type found for event ID {eventId}");

        var policy = ForEvent<EmptyContext>(type);
        return policy.CanGameEventRunLocally(default);
    }

    public bool CanGameEventRunLocally(int eventId, IntPtr context)
    {
        var type = registry.GetComponentType(eventId);

        if (type == null)
            throw new ArgumentException($"No component type found for event ID {eventId}");

        if (!entityMapper.IsMapped(context, out var entity))
        {
            logger.LogError("Failed to map entity context {Context} for native Game event with id {EventId} and type {eventType}", context, eventId, type.FullName);
            return false;
        }

        return ForEvent<Entity>(type).CanGameEventRunLocally(entity.Value);
    }
}