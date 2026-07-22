using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;
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
    /// This in an optimization to avoid allocating HashSets every tick.
    /// This replaces the old bool OwnsEntity(_) check.
    protected abstract uint SentOwners();

    protected abstract void Send(PlayerId owner, NetDataWriter data, SendContext context);

    /// When true, an additional pass runs that sends deltas of components touched by the API
    /// (ChangedFromApi) to the entity's own owner. Defaults to false; the server overrides it.
    protected virtual bool SendApiDeltasToOwner => false;

    /// Owner-directed send used by the API-to-owner pass. Only invoked when SendApiDeltasToOwner is true.
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
    private static NetDataWriter? _writer;

    protected void OnUpdate(SendContext context)
    {
        var maxPacketSize = GetMaxPacketSize();

        // API-to-owner pass runs first and never clears dirty, so the normal pass below
        // still fires for the same components and performs the single clear at the end.
        // Selection is gated on IsDirty (API changes always imply IsDirty), plus ChangedFromApi.
        if (SendApiDeltasToOwner)
        {
            OnUpdateApiToOwner(context, maxPacketSize);
        }

        if (maxPacketSize == null)
        {
            OnUpdateOneChunk(context);
        }
        else
        {
            OnUpdateChunked(context, maxPacketSize.Value);
        }
    }

    private void OnUpdateOneChunk(SendContext context)
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

                    if (_clearDirty)
                    {
                        comp.ClearDirty();
                    }
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
                                throw new InvalidOperationException($"Component {typeof(T).Name} with NetId {meta.NetId} is too large to fit in a packet even by itself. Max packet size is {maxPacketSize} bytes.");
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

                        if (_clearDirty)
                        {
                            comp.ClearDirty();
                        }

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

    /// Sends deltas of API-touched components (IsDirty && ChangedFromApi) to the entity's own owner.
    /// Never clears dirty: the subsequent normal pass reads the same components and performs the clear.
    private void OnUpdateApiToOwner(SendContext context, int? maxPacketSize)
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

                    if (!comp.IsDirty || !comp.ChangedFromApi)
                        continue;

                    if (maxPacketSize == null)
                    {
                        _writer.Put(meta.NetId);
                        comp.WriteDelta(_writer);
                        continue;
                    }

                    var retried = false;

                    while (true)
                    {
                        var beforeApplyPosition = _writer.Length;

                        _writer.Put(meta.NetId);
                        comp.WriteDelta(_writer);

                        if (_writer.Length > maxPacketSize.Value)
                        {
                            if (retried)
                            {
                                throw new InvalidOperationException($"Component {typeof(T).Name} with NetId {meta.NetId} is too large to fit in a packet even by itself. Max packet size is {maxPacketSize.Value} bytes.");
                            }

                            _writer.SetPosition(beforeApplyPosition);
                            SendToOwner(owner, _writer, context);

                            _writer.Reset();
                            CreatePacketHeader(_writer);
                            retried = true;

                            continue;
                        }

                        break;
                    }
                }
            }

            if (_writer.Length > headerSize)
            {
                SendToOwner(owner, _writer, context);
            }
        }
    }
}