using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
internal class WriteSnapshotJob<T>(NetworkedComponentId componentId)
    : IJob<EntityStore, QueryFilter, Entity?, NetDataWriter>, IJob<Entity, NetDataWriter>
    where T : struct, INetworkedComponent
{
    [ThreadStatic] private static NetDataWriter? _writer;
    [ThreadStatic] private static uint _counter;
    
    public void Execute(EntityStore world, QueryFilter filter, Entity? scopeEntity, NetDataWriter writer)
    {
        var begin = writer.Length;
        writer.Put(componentId);

        var position = writer.Length;
        writer.Put((uint)0);

        uint counter = 0;

        if (scopeEntity != null)
        {
            if (scopeEntity.Value.TryGetComponent<T>(out var comp))
            {
                counter++;
                writer.Put(scopeEntity.Value.GetComponent<MetadataComponent>().NetId);
                writer.Put(comp);
            }
        }
        
        var query = world.Query<MetadataComponent, T>(filter);

        _counter = counter;
        _writer = writer;
        query.ForEachEntity((ref meta, ref comp, _) =>
        {
            _counter++;
            _writer.Put(meta.NetId);
            _writer.Put(comp);
        });
        counter = _counter;
        writer = _writer;

        if (counter == 0)
        {
            writer.SetPosition(begin);
            return;
        }

        var finalPosition = writer.Length;
        writer.SetPosition(position);
        writer.Put(counter);

        // Reset position to the end of the data
        writer.SetPosition(finalPosition);
    }

    public void Execute(Entity entity, NetDataWriter writer)
    {
        if (entity.TryGetComponent<T>(out var comp))
        {
            writer.Put(componentId);
            writer.Put((uint)1);

            var meta = entity.GetComponent<MetadataComponent>();
            writer.Put(meta.NetId);
            writer.Put(comp);
        }
    }
}