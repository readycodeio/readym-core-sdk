using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public abstract class SendComponentDeltaSystemBase<T, TContext>(NetworkedComponentId componentId)
    : QuerySystem<MetadataComponent, T> where T : struct, INetworkedComponent
{
    protected abstract int GetMaxPacketSize();
    protected abstract ArchetypeQuery<MetadataComponent, T> GetQuery(TContext? context);
    protected abstract bool OwnsEntity(MetadataComponent meta, TContext? context);
    protected abstract void Send(NetDataWriter data, TContext? context);

    private void CreatePacketHeader(NetDataWriter writer)
    {
        writer.Put((byte)RelayMessageCode.EcsUpdate);
        writer.Put(componentId);
    }
    
    // ReSharper disable once StaticMemberInGenericType
    [ThreadStatic] private static NetDataWriter? _writer;

    protected override void OnUpdate()
        => OnUpdate(default);

    protected void OnUpdate(TContext? context)
    {
        _writer ??= new NetDataWriter();

        _writer.Reset();
        CreatePacketHeader(_writer);
        var headerSize = _writer.Length;

        var query = GetQuery(context);

        query.ForEachEntity((ref MetadataComponent meta, ref T comp, Entity _) =>
        {
            if (!OwnsEntity(meta, context))
            {
                // Skip entities not owned by this peer
                return;
            }

            var retried = false;

            while (true)
            {
                if (!comp.IsDirty)
                    return;
                
                var beforeApplyPosition = _writer.Length;
                
                _writer.Put(meta.NetId);

                comp.WriteDelta(_writer);

                if (_writer.Length > GetMaxPacketSize())
                {
                    if (retried)
                    {
                        // if we retried and still failed, log an error
                        throw new Exception("Packet too large, unable to send");
                    }

                    // Rewind and send the partial packet
                    _writer.SetPosition(beforeApplyPosition);
                    Send(_writer, context);

                    // Start a new writer and retry
                    _writer.Reset();
                    CreatePacketHeader(_writer);
                    retried = true;

                    // Continue loop to retry
                    continue;
                }

                comp.ClearDirty();

                break;
            }
        });

        if (_writer.Length > headerSize)
        {
            Send(_writer, context);
        }
    }
}

public abstract class SendComponentDeltaSystemBase<T>(NetworkedComponentId componentId)
    : SendComponentDeltaSystemBase<T, EmptyContext>(componentId)
    where T : struct, INetworkedComponent
{
    // empty
}