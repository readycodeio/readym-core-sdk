using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

/// A query over every entity of one archetype or mixin.
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

    public Enumerator GetEnumerator() => new(_query, _api);

    public struct Enumerator : IDisposable
    {
        private readonly IEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal Enumerator(ArchetypeQuery query, IEntityApi api)
        {
            _api = api;
            _buffer = EntityBuffer.Fill(query);
            _index = -1;
        }

        public T Current => new()
        {
            Handle = new EntityHandle(_buffer[_index], _api)
        };

        public bool MoveNext() => ++_index < _buffer.Count;

        public void Dispose() => _buffer.Return();
    }
}
