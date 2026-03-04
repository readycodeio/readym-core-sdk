using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public abstract class SendComponentDeltaSystemBase<T> : QuerySystem<MetadataComponent, T> where T : struct, INetworkedComponent
{
    private readonly NetworkedComponentId _componentId;
    private readonly QueryCacheHelper<SendContext, Entity?, ArchetypeQuery<MetadataComponent, T>> _queryCache;

    protected SendComponentDeltaSystemBase(NetworkedComponentId componentId)
    {
        _componentId = componentId;
        _queryCache = new QueryCacheHelper<SendContext, Entity?, ArchetypeQuery<MetadataComponent, T>>(
            context => context.ScopeEntity,
            context =>
            {
                var filter = new QueryFilter();
                filter = SetupFilter(filter, context);
                var query = Query.Store.Query<MetadataComponent, T>(filter);
                return query;
            }
        );
    }

    /// <returns>null if unbound, otherwise the max packet size in bytes</returns>
    protected abstract int? GetMaxPacketSize();

    protected abstract QueryFilter SetupFilter(QueryFilter filter, SendContext context);

    /// Returns a bitmask of player IDs to send to. Bit 0 = PlayerId 0, Bit 1 = PlayerId 1, etc.
    /// This in an optimization to avoid allocating HashSets every tick.
    /// This replaces the old bool OwnsEntity(_) check.
    protected abstract uint SentOwners();

    protected abstract void Send(PlayerId owner, NetDataWriter data, SendContext context);

    private void CreatePacketHeader(NetDataWriter writer)
    {
        writer.Put((byte)RelayMessageCode.EcsDelta);
        writer.Put(_componentId);
    }

    protected override void OnUpdate()
        => OnUpdate(default);

    // ReSharper disable once StaticMemberInGenericType
    [ThreadStatic]
    private static NetDataWriter? _writer;

    protected void OnUpdate(SendContext context)
    {
        var maxPacketSize = GetMaxPacketSize();
        if (maxPacketSize.HasValue)
        {
            OnUpdateChunked(context, maxPacketSize.Value);
        }
        else
        {
            OnUpdateReliable(context);
        }
    }

    private void OnUpdateReliable(SendContext context)
    {
        var owners = SentOwners();

        var query = _queryCache.GetQuery(context);

        for (var ownerIx = 0; ownerIx < 32; ownerIx++)
        {
            if ((owners & (1 << ownerIx)) == 0)
                continue;

            var owner = new PlayerId((ushort)ownerIx);

            _writer ??= new NetDataWriter();

            _writer.Reset();
            CreatePacketHeader(_writer);
            var headerSize = _writer.Length;

            foreach (var (metaChunk, compChunk, _) in query.Chunks)
            {
                var metaSpan = metaChunk.Span;
                var compSpan = compChunk.Span;

                for (var i = 0; i < metaChunk.Length; i++)
                {
                    var meta = metaSpan[i];
                    ref var comp = ref compSpan[i];

                    if (meta.Owner != owner)
                    {
                        continue;
                    }

                    if (!comp.IsDirty)
                        continue;

                    _writer.Put(meta.NetId);
                    comp.WriteDelta(_writer);
                    comp.ClearDirty();
                }
            }

            if (_writer.Length > headerSize)
            {
                Send(owner, _writer, context);
            }
        }
    }

    private void OnUpdateChunked(SendContext context, int maxPacketSize)
    {
        var owners = SentOwners();

        var query = _queryCache.GetQuery(context);

        for (var ownerIx = 0; ownerIx < 32; ownerIx++)
        {
            if ((owners & (1 << ownerIx)) == 0)
                continue;

            var owner = new PlayerId((ushort)ownerIx);

            _writer ??= new NetDataWriter();

            _writer.Reset();
            CreatePacketHeader(_writer);
            var headerSize = _writer.Length;

            foreach (var (metaChunk, compChunk, _) in query.Chunks)
            {
                var metaSpan = metaChunk.Span;
                var compSpan = compChunk.Span;

                for (var i = 0; i < metaChunk.Length; i++)
                {
                    var meta = metaSpan[i];
                    ref var comp = ref compSpan[i];

                    if (meta.Owner != owner)
                    {
                        continue;
                    }

                    var retried = false;

                    while (true)
                    {
                        if (!comp.IsDirty)
                            break;

                        var beforeApplyPosition = _writer.Length;

                        _writer.Put(meta.NetId);

                        comp.WriteDelta(_writer);

                        if (_writer.Length > maxPacketSize)
                        {
                            if (retried)
                            {
                                // if we retried and still failed, log an error
                                throw new Exception("Packet too large, unable to send");
                            }

                            // Rewind and send the partial packet
                            _writer.SetPosition(beforeApplyPosition);
                            Send(owner, _writer, context);

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
                }
            }

            if (_writer.Length > headerSize)
            {
                Send(owner, _writer, context);
            }
        }
    }
}