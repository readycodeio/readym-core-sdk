using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

public readonly ref struct QueryBuilder<T> where T : struct, IArchetypeQueryable
{
    private readonly ArchetypeQuery _query;
    private readonly IEntityApi _api;

    internal QueryBuilder(EntityStore store, IEntityApi api)
    {
        _query = store.Query(new QueryFilter().AllComponents(ClientComponents.Resolve(default(T).Components)));
        _api = api;
    }

    private QueryBuilder(ArchetypeQuery query, IEntityApi api)
    {
        _query = query;
        _api = api;
    }

    public void ForEach(Action<T> action)
    {
        foreach (var component in this)
        {
            action(component);
        }
    }

    public QueryBuilder<T> With<TTag>() where TTag : struct, ITag
    {
        var newQuery = _query.AllTags(Tags.Get<TTag>());
        return new QueryBuilder<T>(newQuery, _api);
    }

    public QueryBuilder<T> Without<TTag>() where TTag : struct, ITag
    {
        var newQuery = _query.WithoutAnyTags(Tags.Get<TTag>());
        return new QueryBuilder<T>(newQuery, _api);
    }

    public struct Enumerator(ArchetypeQuery query, IEntityApi api) : IDisposable
    {
        private EntitiesEnumerator _enumerator = query.Entities.GetEnumerator();

        public T Current => new()
        {
            Handle = new EntityHandle(_enumerator.Current.RawEntity, api)
        };

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Dispose()
        {
            _enumerator.Dispose();
            api.Playback();
        }
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(_query, _api);
    }
}