using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Entity;

public readonly ref struct QueryBuilder<T> where T : struct, IArchetype
{
    private readonly ArchetypeQuery _query;
    private readonly IEntityApi _api;

    public QueryBuilder(EntityStore store, IEntityApi api)
    {
        _query = store.Query(new QueryFilter().AllComponents(default(T).ComponentTypes));
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