using System.ComponentModel;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
#if NET
using ReadyM.SDK.Chunks;
#endif

namespace ReadyM.SDK.Client.Entity;

/// Every entity carrying all 2 shapes, without an archetype that names them.
public readonly struct EntityQuery<T1, T2>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
{
    private readonly ClientEntityContext _context;

    private static readonly ComponentTypes Components = new()
    {
        ClientComponents.Resolve(default(T1).Components),
        ClientComponents.Resolve(default(T2).Components)
    };

    internal EntityQuery(ClientEntityContext context) => _context = context;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_context, Components);

    public void ForEach(Action<T1, T2> body)
    {
        var entities = Identities();

        while (entities.MoveNext())
        {
            var (c1, c2) = entities.Current;
            body(c1, c2);
        }

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
            _buffer = EntityBuffer.Fill(context.Store.Query(new QueryFilter().AllComponents(components)));

            context.Api.EnterQuery();

            _api = context.Api;
            _index = -1;
        }

        public readonly (T1, T2) Current
        {
            get
            {
                var handle = new EntityHandle(_buffer[_index], _api);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle });
            }
        }

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose()
        {
            _buffer.Return();
            _api.LeaveQuery();
        }
    }
}

/// Every entity carrying all 3 shapes, without an archetype that names them.
public readonly struct EntityQuery<T1, T2, T3>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
{
    private readonly ClientEntityContext _context;

    private static readonly ComponentTypes Components = new()
    {
        ClientComponents.Resolve(default(T1).Components),
        ClientComponents.Resolve(default(T2).Components),
        ClientComponents.Resolve(default(T3).Components)
    };

    internal EntityQuery(ClientEntityContext context) => _context = context;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_context, Components);

    public void ForEach(Action<T1, T2, T3> body)
    {
        var entities = Identities();

        while (entities.MoveNext())
        {
            var (c1, c2, c3) = entities.Current;
            body(c1, c2, c3);
        }

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
            _buffer = EntityBuffer.Fill(context.Store.Query(new QueryFilter().AllComponents(components)));

            context.Api.EnterQuery();

            _api = context.Api;
            _index = -1;
        }

        public readonly (T1, T2, T3) Current
        {
            get
            {
                var handle = new EntityHandle(_buffer[_index], _api);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle },
                    new T3 { Handle = handle });
            }
        }

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose()
        {
            _buffer.Return();
            _api.LeaveQuery();
        }
    }
}

/// Every entity carrying all 4 shapes, without an archetype that names them.
public readonly struct EntityQuery<T1, T2, T3, T4>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
{
    private readonly ClientEntityContext _context;

    private static readonly ComponentTypes Components = new()
    {
        ClientComponents.Resolve(default(T1).Components),
        ClientComponents.Resolve(default(T2).Components),
        ClientComponents.Resolve(default(T3).Components),
        ClientComponents.Resolve(default(T4).Components)
    };

    internal EntityQuery(ClientEntityContext context) => _context = context;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_context, Components);

    public void ForEach(Action<T1, T2, T3, T4> body)
    {
        var entities = Identities();

        while (entities.MoveNext())
        {
            var (c1, c2, c3, c4) = entities.Current;
            body(c1, c2, c3, c4);
        }

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
            _buffer = EntityBuffer.Fill(context.Store.Query(new QueryFilter().AllComponents(components)));

            context.Api.EnterQuery();

            _api = context.Api;
            _index = -1;
        }

        public readonly (T1, T2, T3, T4) Current
        {
            get
            {
                var handle = new EntityHandle(_buffer[_index], _api);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle },
                    new T3 { Handle = handle },
                    new T4 { Handle = handle });
            }
        }

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose()
        {
            _buffer.Return();
            _api.LeaveQuery();
        }
    }
}

/// Every entity carrying all 5 shapes, without an archetype that names them.
public readonly struct EntityQuery<T1, T2, T3, T4, T5>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
{
    private readonly ClientEntityContext _context;

    private static readonly ComponentTypes Components = new()
    {
        ClientComponents.Resolve(default(T1).Components),
        ClientComponents.Resolve(default(T2).Components),
        ClientComponents.Resolve(default(T3).Components),
        ClientComponents.Resolve(default(T4).Components),
        ClientComponents.Resolve(default(T5).Components)
    };

    internal EntityQuery(ClientEntityContext context) => _context = context;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_context, Components);

    public void ForEach(Action<T1, T2, T3, T4, T5> body)
    {
        var entities = Identities();

        while (entities.MoveNext())
        {
            var (c1, c2, c3, c4, c5) = entities.Current;
            body(c1, c2, c3, c4, c5);
        }

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
            _buffer = EntityBuffer.Fill(context.Store.Query(new QueryFilter().AllComponents(components)));

            context.Api.EnterQuery();

            _api = context.Api;
            _index = -1;
        }

        public readonly (T1, T2, T3, T4, T5) Current
        {
            get
            {
                var handle = new EntityHandle(_buffer[_index], _api);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle },
                    new T3 { Handle = handle },
                    new T4 { Handle = handle },
                    new T5 { Handle = handle });
            }
        }

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose()
        {
            _buffer.Return();
            _api.LeaveQuery();
        }
    }
}

/// Every entity carrying all 6 shapes, without an archetype that names them.
public readonly struct EntityQuery<T1, T2, T3, T4, T5, T6>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
    where T6 : struct, IArchetypeMixin
{
    private readonly ClientEntityContext _context;

    private static readonly ComponentTypes Components = new()
    {
        ClientComponents.Resolve(default(T1).Components),
        ClientComponents.Resolve(default(T2).Components),
        ClientComponents.Resolve(default(T3).Components),
        ClientComponents.Resolve(default(T4).Components),
        ClientComponents.Resolve(default(T5).Components),
        ClientComponents.Resolve(default(T6).Components)
    };

    internal EntityQuery(ClientEntityContext context) => _context = context;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_context, Components);

    public void ForEach(Action<T1, T2, T3, T4, T5, T6> body)
    {
        var entities = Identities();

        while (entities.MoveNext())
        {
            var (c1, c2, c3, c4, c5, c6) = entities.Current;
            body(c1, c2, c3, c4, c5, c6);
        }

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
            _buffer = EntityBuffer.Fill(context.Store.Query(new QueryFilter().AllComponents(components)));

            context.Api.EnterQuery();

            _api = context.Api;
            _index = -1;
        }

        public readonly (T1, T2, T3, T4, T5, T6) Current
        {
            get
            {
                var handle = new EntityHandle(_buffer[_index], _api);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle },
                    new T3 { Handle = handle },
                    new T4 { Handle = handle },
                    new T5 { Handle = handle },
                    new T6 { Handle = handle });
            }
        }

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose()
        {
            _buffer.Return();
            _api.LeaveQuery();
        }
    }
}
