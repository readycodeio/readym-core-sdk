using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Server.Entity;

public readonly ref struct QueryBuilder<T> where T : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;
    private readonly ComponentSet _components;

    internal QueryBuilder(ServerEntityApi api)
    {
        _api = api;
        _components = default(T).Components;
    }

    public Enumerator GetEnumerator() => new(_api, _api.CollectMatching(_components));

    public void ForEach(Action<T> action)
    {
        foreach (var entity in this)
        {
            action(entity);
        }
    }
    
    // TODO
    public QueryBuilder<T> With<TTag>() where TTag : struct, ITag
        => throw new NotSupportedException($"Cannot filter by {typeof(TTag).Name}: the server has no tags.");

    // TODO
    public QueryBuilder<T> Without<TTag>() where TTag : struct, ITag
        => throw new NotSupportedException($"Cannot filter by {typeof(TTag).Name}: the server has no tags.");

    public struct Enumerator : IDisposable
    {
        private readonly ServerEntityApi _api;
        private readonly EntityIdBuffer _buffer;
        private int _index;

        internal Enumerator(ServerEntityApi api, EntityIdBuffer buffer)
        {
            _api = api;
            _buffer = buffer;
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
