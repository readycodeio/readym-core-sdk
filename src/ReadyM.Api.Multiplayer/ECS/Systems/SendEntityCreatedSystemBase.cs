using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public abstract class SendEntityCreatedSystemBase<TContext> : QuerySystem<MetadataComponent>
{
    protected abstract ArchetypeQuery<MetadataComponent> GetQuery(TContext? context);
    protected abstract void Send(NetDataWriter writer, TContext? context);

    protected SendEntityCreatedSystemBase()
    {
        Filter.AllTags(Tags.Get<LocallyCreatedEntityTag>());
    }

    private static void CreatePacketHeader(NetDataWriter writer)
    {
        writer.Put((byte)RelayMessageCode.EcsCreateEntity);
    }

    protected override void OnUpdate()
        => OnUpdate(default);

    // ReSharper disable once StaticMemberInGenericType
    [ThreadStatic] private static NetDataWriter? _writer;
    
    protected void OnUpdate(TContext? context)
    {
        _writer ??= new NetDataWriter();
        
        _writer.Reset();
        CreatePacketHeader(_writer);
        var headerSize = _writer.Length;

        var query = GetQuery(context);
        
        query.ForEachEntity((ref MetadataComponent meta, Entity entity) =>
        {
            _writer.Put(meta);

            CommandBuffer.RemoveTag<LocallyCreatedEntityTag>(entity.Id);
        });

        if (_writer.Length > headerSize)
        {
            Send(_writer, context);
        }
    }
}

public abstract class SendEntityCreatedSystemBase : SendEntityCreatedSystemBase<EmptyContext>
{
    // empty
}