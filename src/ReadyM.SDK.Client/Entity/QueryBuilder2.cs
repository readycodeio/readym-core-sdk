using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

public readonly ref struct QueryBuilder<T1, T2>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeQueryable
{
    private readonly ArchetypeQuery _query;
    private readonly IEntityApi _api;

    internal QueryBuilder(EntityStore store, IEntityApi api)
    {
        _query = store.Query(new QueryFilter().AllComponents(new ComponentTypes
        {
            ClientComponents.Resolve(default(T1).Components),
            ClientComponents.Resolve(default(T2).Components)
        }));
        _api = api;
    }

    private QueryBuilder(ArchetypeQuery query, IEntityApi api)
    {
        _query = query;
        _api = api;
    }

    public void ForEach(Action<T1, T2> action)
    {
        foreach (var (c1, c2) in this)
        {
            action(c1, c2);
        }
    }

    public QueryBuilder<T1, T2> With<TTag>() where TTag : struct, ITag
    {
        var newQuery = _query.AllTags(Tags.Get<TTag>());
        return new QueryBuilder<T1, T2>(newQuery, _api);
    }

    public QueryBuilder<T1, T2> Without<TTag>() where TTag : struct, ITag
    {
        var newQuery = _query.WithoutAnyTags(Tags.Get<TTag>());
        return new QueryBuilder<T1, T2>(newQuery, _api);
    }

    public struct Enumerator : IDisposable
    {
        private readonly IEntityApi _api;
        private EntitiesEnumerator _enumerator;

        internal Enumerator(ArchetypeQuery query, IEntityApi api)
        {
            _api = api;
            _enumerator = query.Entities.GetEnumerator();
        }

        public (T1, T2) Current => (
            new T1 { Handle = new EntityHandle(_enumerator.Current.RawEntity, _api) },
            new T2 { Handle = new EntityHandle(_enumerator.Current.RawEntity, _api) }
        );

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Dispose()
        {
            _enumerator.Dispose();
            _api.Playback();
        }
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(_query, _api);
    }
}