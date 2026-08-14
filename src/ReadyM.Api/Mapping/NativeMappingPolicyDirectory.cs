using System;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Mapping;

internal class NativeMappingPolicyDirectory(
    DataSideChannel sideChannel,
    INativeComponentRegistry registry,
    IMappedEntityManager<IntPtr> entityMapper,
    ECS.Worlds.Store world,
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
    
    public bool ShouldGameCopyToEcs(int componentId, IntPtr context)
        => TryResolveData(componentId, context, out var policy, out var entity) && policy.ShouldGameCopyToEcs(entity);

    public bool ShouldEcsCopyToGame(int componentId, IntPtr context)
        => TryResolveData(componentId, context, out var policy, out var entity) && policy.ShouldEcsCopyToGame(entity);

    public bool CanSetFromApi(int componentId, IntPtr context)
        => TryResolveData(componentId, context, out var policy, out var entity) && policy.CanSetFromApi(entity);

    public bool CanGameSetLocally(int componentId, IntPtr context)
        => TryResolveData(componentId, context, out var policy, out var entity) && policy.CanGameSetLocally(entity);

    public bool ShouldGameCopyToEcsForEntity(int componentId, int entityId)
        => TryResolveDataForEntity(componentId, entityId, out var policy, out var entity) && policy.ShouldGameCopyToEcs(entity);

    public bool ShouldEcsCopyToGameForEntity(int componentId, int entityId)
        => TryResolveDataForEntity(componentId, entityId, out var policy, out var entity) && policy.ShouldEcsCopyToGame(entity);

    public bool CanSetFromApiForEntity(int componentId, int entityId)
        => TryResolveDataForEntity(componentId, entityId, out var policy, out var entity) && policy.CanSetFromApi(entity);

    public bool CanGameSetLocallyForEntity(int componentId, int entityId)
        => TryResolveDataForEntity(componentId, entityId, out var policy, out var entity) && policy.CanGameSetLocally(entity);

    private bool TryResolveDataForEntity(int componentId, int entityId, out IMappingDataPolicy<Entity> policy, out Entity entity)
    {
        policy = null!;
        entity = default;

        var type = registry.GetComponentType(componentId);
        if (type == null)
            throw new ArgumentException($"No component type found for component ID {componentId}");

        if (!world.TryGetEntityById(entityId, out entity))
        {
            logger.LogError("Failed to resolve entity {EntityId} for native data policy of component id {ComponentId} type {Type}", entityId, componentId, type.FullName);
            return false;
        }

        policy = ForData(type);
        return true;
    }

    private bool TryResolveData(int componentId, IntPtr context, out IMappingDataPolicy<Entity> policy, out Entity entity)
    {
        policy = null!;
        entity = default;

        var type = registry.GetComponentType(componentId);
        if (type == null)
            throw new ArgumentException($"No component type found for component ID {componentId}");

        if (!entityMapper.IsMapped(context, out var mapped))
        {
            logger.LogError("Failed to map entity context {Context} for native data policy of component id {ComponentId} type {Type}", context, componentId, type.FullName);
            return false;
        }

        policy = ForData(type);
        entity = mapped.Value;
        return true;
    }

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