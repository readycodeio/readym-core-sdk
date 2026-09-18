using System.ComponentModel;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Chunks;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Server.Entity;

/// Every entity of a shape. How it is walked is decided for you: a shape whose components are all
/// known at compile time is walked by chunk, and anything else by identity. The choice is made by
/// overload resolution, so this type has no GetEnumerator of its own and a foreach binds one of the
/// extensions instead.
public readonly ref struct EntityQuery<T>
    where T : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;

    private static ComponentSet Components => default(T).Components;

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_api, Components);

    /// Narrows the query to the entities one scope holds.
    public ScopedQuery<T> InScope(Scope scope) => new(_api, scope);

    /// Runs a body over every entity, walking identities so it may change the world.
    public void ForEach(Action<T> body)
    {
        var entities = Identities();

        while (entities.MoveNext())
            body(entities.Current);

        entities.Dispose();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct IdentityEnumerator : IDisposable
    {
        private readonly ServerEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal IdentityEnumerator(ServerEntityApi api, ComponentSet components)
        {
            // Collected before the scope opens, so a failure here cannot leave one open.
            _buffer = api.CollectMatching(components);

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
