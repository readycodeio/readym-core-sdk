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
    : IJob<EntityStore, QueryFilter, Entity?, NetDataWriter>
    where T : struct, INetworkedComponent
{
    [ThreadStatic] private static NetDataWriter? _writer;
    [ThreadStatic] private static uint _counter;
    
    public void Execute(EntityStore world, QueryFilter filter, Entity? scopeEntity, NetDataWriter writer)
    {
        _writer = writer;
        _counter = 0;
        
        var begin = _writer.Length;
        _writer.Put(componentId);

        var countPosition = _writer.Length;
        _writer.Put((uint)0);

        if (scopeEntity != null)
        {
            if (scopeEntity.Value.TryGetComponent<T>(out var comp))
            {
                _counter++;
                _writer.Put(scopeEntity.Value.GetComponent<MetadataComponent>().NetId);
                _writer.Put(comp);
            }
        }
        
        var query = world.Query<MetadataComponent, T>(filter);

        query.ForEachEntity(static (ref meta, ref comp, _) =>
        {
            _counter++;
            _writer.Put(meta.NetId);
            _writer.Put(comp);
        });

        if (_counter == 0)
        {
            _writer.SetPosition(begin);
            return;
        }

        var finalPosition = _writer.Length;
        _writer.SetPosition(countPosition);
        _writer.Put(_counter);

        // Reset position to the end of the data
        _writer.SetPosition(finalPosition);
    }
}