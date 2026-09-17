using System.ComponentModel;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
#if NET
using ReadyM.SDK.Chunks;
#endif

namespace ReadyM.SDK.Client.Entity;

/// Every entity of a shape. How it is walked is decided for you: a shape whose components are all
/// known at compile time is walked by chunk, and anything else by identity. The choice is made by
/// overload resolution, so this type has no GetEnumerator of its own and a foreach binds one of the
/// extensions instead. A netstandard2.0 client has no chunk path at all, so every query there walks
/// identities.
public readonly struct EntityQuery<T>
    where T : struct, IArchetypeQueryable
{
    private readonly ClientEntityContext _context;

    private static ComponentTypes Components => ClientComponents.Resolve(default(T).Components);

    internal EntityQuery(ClientEntityContext context) => _context = context;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_context, Components);

    /// Runs a body over every entity, walking identities so it may change the world.
    public void ForEach(Action<T> body)
    {
        var entities = Identities();

        while (entities.MoveNext())
            body(entities.Current);

        entities.Dispose();
    }

#if NET
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_context.ChunkSource).GetEnumerator();
#endif

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct IdentityEnumerator : IDisposable
    {
        private readonly IEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal IdentityEnumerator(ClientEntityContext context, ComponentTypes components)
        {
            // Filled before the scope opens, so a failure here cannot leave one open.
            _buffer = EntityBuffer.Fill(context.Store.Query(new QueryFilter().AllComponents(components)));

            context.Api.EnterQuery();

            _api = context.Api;
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
