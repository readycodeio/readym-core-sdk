using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

internal abstract class SendComponentDeltaSystemBase<T> : QuerySystem<MetadataComponent, T>
    where T : struct, INetworkedComponent
{
    private readonly NetworkedComponentId _componentId;
    private readonly bool _clearDirty;
    private readonly QueryCacheHelper<SendContext, Entity?, ArchetypeQuery<MetadataComponent, T>> _queryCache;

    protected SendComponentDeltaSystemBase(NetworkedComponentId componentId, bool clearDirty)
    {
        _componentId = componentId;
        _clearDirty = clearDirty;
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
    /// This is an optimization to avoid allocating HashSets every tick.
    protected abstract uint SentOwners();

    /// Sends a packet of an owner's deltas to everyone in scope EXCEPT that owner.
    protected abstract void SendExceptOwner(PlayerId owner, NetDataWriter data, SendContext context);

    /// When true, components touched by the API (ChangedFromApi) additionally have their delta
    /// sent to the entity's own owner, so the server can authoritatively override the owner's state.
    /// Defaults to false; the server overrides it.
    protected virtual bool SendApiDeltasToOwner => false;

    /// Owner-directed send used for API-authored deltas. Only invoked when SendApiDeltasToOwner is true.
    protected virtual void SendToOwner(PlayerId owner, NetDataWriter data, SendContext context) { }

    private void CreatePacketHeader(NetDataWriter writer)
    {
        writer.Put((byte)RelayMessageCode.EcsDelta);
        writer.Put(_componentId);
    }

    protected override void OnUpdate()
        => OnUpdate(default);

    // ReSharper disable once StaticMemberInGenericType
    [ThreadStatic]
    private static NetDataWriter? _othersWriter;

    // ReSharper disable once StaticMemberInGenericType
    [ThreadStatic]
    private static NetDataWriter? _ownerWriter;

    protected void OnUpdate(SendContext context)
    {
        var maxPacketSize = GetMaxPacketSize();
        var owners = SentOwners();
        var query = _queryCache.GetQuery(context);

        for (var ownerIx = 0; ownerIx < 32; ownerIx++)
        {
            if ((owners & (1u << ownerIx)) == 0)
                continue;

            SendForOwner(new PlayerId((ushort)ownerIx), query, maxPacketSize, context);
        }
    }

    private void SendForOwner(
        PlayerId owner,
        ArchetypeQuery<MetadataComponent, T> query,
        int? maxPacketSize,
        SendContext context)
    {
        // "others" packet: this owner's deltas relayed to everyone else in scope (the normal path).
        var others = ResetWriter(ref _othersWriter);
        var othersHeaderSize = others.Length;

        // "owner" packet: API-authored deltas sent back to the owner. Lazily headered only if used,
        // so it costs nothing on the common path where no component is ChangedFromApi.
        NetDataWriter? owned = null;
        var ownedHeaderSize = 0;

        foreach (var (metaChunk, compChunk, _) in query.Chunks)
        {
            var metaSpan = metaChunk.Span;
            var compSpan = compChunk.Span;

            for (var i = 0; i < metaChunk.Length; i++)
            {
                var meta = metaSpan[i];
                ref var comp = ref compSpan[i];

                if (meta.Owner != owner)
                    continue;

                if (!comp.IsDirty)
                    continue;

                // API-authored deltas additionally go back to the owner (single scan, no second pass).
                if (SendApiDeltasToOwner && comp.ChangedFromApi)
                {
                    if (owned == null)
                    {
                        owned = ResetWriter(ref _ownerWriter);
                        ownedHeaderSize = owned.Length;
                    }

                    AppendDelta(owned, meta.NetId, ref comp, maxPacketSize, 
                        flush: w => SendToOwner(owner, w, context));
                }

                AppendDelta(others, meta.NetId, ref comp, maxPacketSize,
                    flush: w => SendExceptOwner(owner, w, context));

                if (_clearDirty)
                    comp.ClearDirty();
            }
        }

        if (others.Length > othersHeaderSize)
            SendExceptOwner(owner, others, context);

        if (owned != null && owned.Length > ownedHeaderSize)
            SendToOwner(owner, owned, context);
    }

    /// Writes one entity's delta into <paramref name="writer"/>. On a bounded (unreliable) channel,
    /// if the delta would overflow the packet the buffer is rewound, the partial packet is flushed,
    /// a fresh header is written, and the delta is retried once. On an unbounded (reliable) channel
    /// no size cap applies and the delta is written directly (the one-chunk behaviour).
    private void AppendDelta(
        NetDataWriter writer,
        NetworkId netId,
        ref T comp,
        int? maxPacketSize,
        Action<NetDataWriter> flush)
    {
        if (maxPacketSize == null)
        {
            writer.Put(netId);
            comp.WriteDelta(writer);
            return;
        }

        var cap = maxPacketSize.Value;
        var retried = false;

        while (true)
        {
            var beforePosition = writer.Length;

            writer.Put(netId);
            comp.WriteDelta(writer);

            if (writer.Length <= cap)
                return;

            if (retried)
                throw new InvalidOperationException($"Component {typeof(T).Name} with NetId {netId} is too large to fit in a packet even by itself. Max packet size is {cap} bytes.");

            // Rewind the overflowing delta, flush the partial packet, start a fresh one and retry.
            writer.SetPosition(beforePosition);
            flush(writer);

            writer.Reset();
            CreatePacketHeader(writer);
            retried = true;
        }
    }

    private NetDataWriter ResetWriter(ref NetDataWriter? slot)
    {
        slot ??= new NetDataWriter();
        slot.Reset();
        CreatePacketHeader(slot);
        return slot;
    }
}