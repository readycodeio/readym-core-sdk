using System;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Helpers;
using ReadyM.Api.Interop;
using ReadyM.Api.Interop.Registry;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping.Events;

internal class NativeMappedEventManager(
    DataSideChannel sideChannel,
    INativeMappingPolicyDirectory policyDir,
    INativeComponentRegistry nativeRegistry,
    IMappedEntityManager<IntPtr> entityMapper,
    ILogger logger
) : MappedEventManager(sideChannel, policyDir, logger)
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

    public void RegisterNativeGameEventHandler(int eventId, ClosureTrampoline1 callback)
    {
        var eventType = nativeRegistry.GetComponentType(eventId);

        if (eventType is null || !typeof(IInteropType).IsAssignableFrom(eventType))
        {
            logger.LogError("Attempted to register native GAME event callback for unknown event ID {EventId}", eventId);
            return;
        }

        incomingGameEventQueue.RegisterOpaqueHandler(eventType, static (ev, cb) =>
        {
            var handle = GCHandle.Alloc(ev, GCHandleType.Pinned);
            try
            {
                cb.Invoke(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }, callback);
    }

    public void RegisterNativeEcsEventHandler(int eventId, ClosureTrampoline1 callback)
    {
        var eventType = nativeRegistry.GetComponentType(eventId);

        if (eventType is null || !typeof(IInteropType).IsAssignableFrom(eventType))
        {
            logger.LogError("Attempted to register native ECS event callback for unknown event ID {EventId}", eventId);
            return;
        }

        incomingEcsEventQueue.RegisterOpaqueHandler(eventType, (ev, cb) =>
        {
            var handle = GCHandle.Alloc(ev, GCHandleType.Pinned);
            try
            {
                cb.Invoke(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }, callback);
    }

    public bool NotifyEcsIfApplicable(int eventId, IntPtr data)
    {
        var eventType = nativeRegistry.GetComponentType(eventId);
        if (eventType is null)
        {
            logger.LogError("Attempted to notify ECS of unknown event with id {EventId}", eventId);
            return false;
        }

        if (!typeof(IAlwaysPropagates).IsAssignableFrom(eventType) && !typeof(IAlwaysPropagatesToEcsOnly).IsAssignableFrom(eventType))
        {
            logger.LogError("Attempted to notify ECS of event with id {EventId} and type {EventType} which does not implement IAlwaysPropagates", eventId, eventType.FullName);
            return false;
        }

        if (!policyDir.ForEventOpaque<EmptyContext>(eventType).CanGameEventNotifyEcs(default))
            return false;

        using (sideChannel.PushScope(PropagationDirection.ToEcs, eventId))
        {
            var ev = Marshal.PtrToStructure(data, eventType);

            if (ev is not null)
            {
                incomingEcsEventQueue.Invoke(ev, eventType);
            }
            else
            {
                logger.LogError("Failed to marshal native ECS event with id {EventId} and type {eventName}", eventId, eventType.FullName);
            }
        }

        return true;
    }

    // TODO: For now, we hard-code IntPtr as context, for IOwnershipBased events
    public bool NotifyEcsIfApplicable(int eventId, IntPtr data, IntPtr context)
    {
        var eventType = nativeRegistry.GetComponentType(eventId);
        if (eventType is null)
        {
            logger.LogError("Attempted to notify ECS of unknown event with id {EventId}", eventId);
            return false;
        }

        if (!typeof(IOwnershipBased).IsAssignableFrom(eventType))
        {
            logger.LogError("Attempted to notify ECS of event with id {EventId} and type {EventType} which does not implement IOwnershipBased", eventId, eventType.FullName);
            return false;
        }

        if (!entityMapper.IsMapped(context, out var entity))
        {
            logger.LogError("Failed to map entity context {Context} for native ECS event with id {EventId} and type {eventType}", context, eventId, eventType.FullName);
            return false;
        }

        if (!policyDir.ForEventOpaque<Entity>(eventType).CanGameEventNotifyEcs(entity.Value))
            return false;

        using (sideChannel.PushScope(PropagationDirection.ToEcs, eventId))
        {
            var ev = Marshal.PtrToStructure(data, eventType);

            if (ev is not null)
            {
                incomingEcsEventQueue.Invoke(ev, eventType);
            }
            else
            {
                logger.LogError("Failed to marshal native ECS event with id {EventId} and type {eventName}", eventId, eventType.FullName);
            }
        }

        return true;
    }

    public bool InvokeInGameIfApplicable(int eventId, IntPtr data)
    {
        var eventType = nativeRegistry.GetComponentType(eventId);
        if (eventType is null)
        {
            logger.LogError("Attempted to invoke in Game an unknown event with id {EventId}", eventId);
            return false;
        }

        if (!typeof(IAlwaysPropagates).IsAssignableFrom(eventType) && !typeof(IAlwaysPropagatesToEcsOnly).IsAssignableFrom(eventType))
        {
            logger.LogError("Attempted to invoke in Game an event with id {EventId} and type {EventType} which does not implement IAlwaysPropagates", eventId, eventType.FullName);
            return false;
        }

        if (!policyDir.ForEventOpaque<EmptyContext>(eventType).CanEcsInvokeGameEvent(default))
            return false;

        using (sideChannel.PushScope(PropagationDirection.ToGame, eventId))
        {
            var ev = Marshal.PtrToStructure(data, eventType);

            if (ev is not null)
            {
                incomingGameEventQueue.Invoke(ev, eventType);
            }
            else
            {
                logger.LogError("Failed to marshal native Game event with id {EventId} and type {eventName}", eventId, eventType.FullName);
            }
        }

        return true;
    }

    // TODO: For now, we hard-code IntPtr as context, for IOwnershipBased events
    public bool InvokeInGameIfApplicable(int eventId, IntPtr data, IntPtr context)
    {
        var eventType = nativeRegistry.GetComponentType(eventId);
        if (eventType is null)
        {
            logger.LogError("Attempted to invoke in Game an unknown event with id {EventId}", eventId);
            return false;
        }

        if (!typeof(IOwnershipBased).IsAssignableFrom(eventType))
        {
            logger.LogError("Attempted to invoke in Game an event with id {EventId} and type {EventType} which does not implement IOwnershipBased", eventId, eventType.FullName);
            return false;
        }

        if (!entityMapper.IsMapped(context, out var entity))
        {
            logger.LogError("Failed to map entity context {Context} for native Game event with id {EventId} and type {eventType}", context, eventId, eventType.FullName);
            return false;
        }

        if (!policyDir.ForEventOpaque<Entity>(eventType).CanEcsInvokeGameEvent(entity.Value))
            return false;

        using (sideChannel.PushScope(PropagationDirection.ToGame, eventId))
        {
            var ev = Marshal.PtrToStructure(data, eventType);

            if (ev is not null)
            {
                incomingGameEventQueue.Invoke(ev, eventType);
            }
            else
            {
                logger.LogError("Failed to marshal native ECS event with id {EventId} and type {eventName}", eventId, eventType.FullName);
            }
        }

        return true;
    }

    public void InvokeInGameAndNotifyEcs(int eventId, IntPtr data)
    {
        NotifyEcsIfApplicable(eventId, data);
        InvokeInGameIfApplicable(eventId, data);
    }

    public void InvokeInGameAndNotifyEcs(int eventId, IntPtr data, IntPtr context)
    {
        NotifyEcsIfApplicable(eventId, data, context);
        InvokeInGameIfApplicable(eventId, data, context);
    }
}