using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Jobs;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

internal abstract class SendEntityCreatedSystemBase : QuerySystem<MetadataComponent>
{
    private readonly JobRegistry _jobRegistry;
    private readonly QueryCacheHelper<SendContext, Entity?, ArchetypeQuery<MetadataComponent>> _queryCache;

    protected SendEntityCreatedSystemBase(JobRegistry jobRegistry)
    {
        _jobRegistry = jobRegistry;
        _queryCache = new(
            context => context.ScopeEntity,
            context =>
            {
                var filter = new QueryFilter();
                filter = SetupFilter(filter, context);
                var query = Query.Store.Query<MetadataComponent>(filter);
                return query;
            }
        );

        SetupBaseFilter(Filter);
    }

    protected abstract QueryFilter SetupFilter(QueryFilter filter, SendContext context);

    protected QueryFilter SetupBaseFilter(QueryFilter filter)
        => filter.AllTags(Friflo.Engine.ECS.Tags.Get<LocallyCreatedEntityTag>());

    protected abstract void CreatePacketHeader(NetDataWriter writer, SendContext context);

    protected abstract void Send(NetDataWriter writer, SendContext context);

    protected override void OnUpdate()
        => OnUpdate(default);

    // ReSharper disable once StaticMemberInGenericType
    [ThreadStatic]
    private static NetDataWriter? _writer;

    protected void OnUpdate(SendContext context)
    {
        _writer ??= new NetDataWriter();

        _writer.Reset();
        CreatePacketHeader(_writer, context);

        var query = _queryCache.GetQuery(context);
        var queryCount = query.Count;

        if (queryCount == 0)
            return;

        _writer.Put(queryCount);
        query.ForEachEntity((ref meta, entity) =>
        {
            MetadataComponent.Serialize(meta, _writer);
            CommandBuffer.RemoveTag<LocallyCreatedEntityTag>(entity.Id);
        });

        _jobRegistry.WriteSnapshot(query.Store, query.Filter, null, _writer);

        if (queryCount > 0)
        {
            Send(_writer, context);
        }
    }
}