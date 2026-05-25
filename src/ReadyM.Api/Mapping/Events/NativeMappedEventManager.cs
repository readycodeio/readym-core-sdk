using System;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Helpers;
using ReadyM.Api.Interop.Registry;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping.Events;

internal class NativeMappedEventManager(
    DataSideChannel sideChannel,
    IMappingPolicyDirectory policyDir,
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

    public void RegisterNativeGameEventHandler(int eventId, NativeEventCallback callback)
    {
        var eventType = nativeRegistry.GetComponentType(eventId);

        if (eventType is null || !typeof(IInteropType).IsAssignableFrom(eventType))
        {
            logger.LogError("Attempted to register native GAME event callback for unknown event ID {EventId}", eventId);
            return;
        }

        if (!callback.IsValid)
        {
            logger.LogError("Attempted to register invalid native GAME event callback for event type {EventType}", eventType.FullName);
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

    public void RegisterNativeEcsEventHandler(int eventId, NativeEventCallback callback)
    {
        var eventType = nativeRegistry.GetComponentType(eventId);

        if (eventType is null || !typeof(IInteropType).IsAssignableFrom(eventType))
        {
            logger.LogError("Attempted to register native ECS event callback for unknown event ID {EventId}", eventId);
            return;
        }

        if (!callback.IsValid)
        {
            logger.LogError("Attempted to register invalid native ECS event callback for event type {EventType}", eventType.FullName);
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
            logger.LogError("Attempted to notify ECS of event with id {EventId} and type {EventType} which does not implement IOwnershipManaged", eventId, eventType.FullName);
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

    // TODO: For now, we hard-code IntPtr as context, for IOwnershipManaged events
    public bool NotifyEcsIfApplicable(int eventId, IntPtr data, IntPtr context)
    {
        var eventType = nativeRegistry.GetComponentType(eventId);
        if (eventType is null)
        {
            logger.LogError("Attempted to notify ECS of unknown event with id {EventId}", eventId);
            return false;
        }

        if (!typeof(IOwnershipManaged).IsAssignableFrom(eventType))
        {
            logger.LogError("Attempted to notify ECS of event with id {EventId} and type {EventType} which does not implement IOwnershipManaged", eventId, eventType.FullName);
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
}