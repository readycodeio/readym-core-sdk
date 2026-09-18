using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

/// Every entity a scope holds that carries the shape. Walked by identity: a scope's entities are
/// scattered through the archetypes they belong to, so there are no chunks to hand back.
public readonly struct ScopedQuery<T> where T : struct, IArchetypeQueryable
{
    private readonly ClientEntityContext _api;
    private readonly Scope _scope;

    internal ScopedQuery(ClientEntityContext api, Scope scope)
    {
        _api = api;
        _scope = scope;
    }

    public Enumerator GetEnumerator() => new(_api.Api, _scope);

    public void ForEach(Action<T> body)
    {
        var entities = GetEnumerator();

        while (entities.MoveNext())
            body(entities.Current);

        entities.Dispose();
    }

    public struct Enumerator : IDisposable
    {
        private readonly IEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal Enumerator(IEntityApi api, Scope scope)
        {
            _buffer = api.CollectInScope(scope.Handle.RawEntity, default(T).Components);

            api.EnterQuery();

            _api = api;
            _index = -1;
        }

        public readonly T Current => new() { Handle = new EntityHandle(_buffer[_index], _api) };

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose()
        {
            _buffer.Return();
            _api.LeaveQuery();
        }
    }
}
