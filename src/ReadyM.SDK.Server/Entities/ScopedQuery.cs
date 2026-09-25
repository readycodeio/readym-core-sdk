using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server.Entities;

/// Every entity a scope holds that carries the shape. Walked by identity: a scope's entities are
/// scattered through the archetypes they belong to, so there are no chunks to hand back.
public readonly ref struct ScopedQuery<T> where T : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi? _api;
    private readonly Scope _scope;

    internal ScopedQuery(ServerEntityApi? api, Scope scope)
    {
        _api = api;
        _scope = scope;
    }

    public Enumerator GetEnumerator() => new(_api, _scope);

    public struct Enumerator : IDisposable
    {
        private readonly IEntityApi? _api;
        private readonly EntityBuffer? _buffer;
        private int _index;

        internal Enumerator(IEntityApi? api, Scope scope)
        {
            // Nothing to look in, or no scope to look at: either way this matches nothing, so a
            // property can hand one out when there is no current scope.
            if (api is null || scope.Handle.RawEntity.Id == 0)
            {
                _api = null;
                _buffer = null;
                _index = -1;
                return;
            }

            _buffer = api.CollectInScope(scope.Handle.RawEntity, default(T).Components);

            api.EnterQuery();

            _api = api;
            _index = -1;
        }

        public readonly T Current => new() { Handle = new EntityHandle(_buffer![_index], _api!) };

        public bool MoveNext() => _buffer is not null && ++_index < _buffer.Count;

        public readonly void Dispose()
        {
            if (_buffer is null)
                return;

            _buffer.Return();
            _api!.LeaveQuery();
        }
    }
}
