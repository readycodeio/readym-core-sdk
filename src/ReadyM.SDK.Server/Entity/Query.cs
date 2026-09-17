using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Server.Entity;

public readonly ref struct Query<T> where T : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;
    private readonly ComponentSet _components;

    internal Query(ServerEntityApi api)
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
    
    public struct Enumerator : IDisposable
    {
        private readonly ServerEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal Enumerator(ServerEntityApi api, EntityBuffer buffer)
        {
            api.EnterQuery();

            _api = api;
            _buffer = buffer;
            _index = -1;
        }

        public T Current => new()
        {
            Handle = new EntityHandle(_buffer[_index], _api)
        };

        public bool MoveNext() => ++_index < _buffer.Count;

        public void Dispose()
        {
            _buffer.Return();
            _api.LeaveQuery();
        }
    }
}
