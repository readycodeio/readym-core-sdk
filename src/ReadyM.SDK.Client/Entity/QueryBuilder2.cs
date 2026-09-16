using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

/// A query over every entity that includes the two mixins.
public readonly ref struct QueryBuilder<T1, T2>
    where T1 : struct, IArchetypeMixin
    where T2 : struct, IArchetypeMixin
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

        public (T1, T2) Current => (
            new T1 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T2 { Handle = new EntityHandle(_buffer[_index], _api) }
        );

        public bool MoveNext() => ++_index < _buffer.Count;

        public void Dispose() => _buffer.Return();
    }
}
