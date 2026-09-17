using System.ComponentModel;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Chunks;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Server.Entity;

/// Every entity carrying all 2 shapes, without an archetype that names them.
public readonly ref struct EntityQuery<T1, T2>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components);

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_api, Components);

    /// Runs a body over every entity, walking identities so it may change the world.
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
public readonly ref struct EntityQuery<T1, T2, T3>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components);

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_api, Components);

    /// Runs a body over every entity, walking identities so it may change the world.
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
public readonly ref struct EntityQuery<T1, T2, T3, T4>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components, default(T4).Components);

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_api, Components);

    /// Runs a body over every entity, walking identities so it may change the world.
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
public readonly ref struct EntityQuery<T1, T2, T3, T4, T5>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components, default(T4).Components, default(T5).Components);

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_api, Components);

    /// Runs a body over every entity, walking identities so it may change the world.
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
public readonly ref struct EntityQuery<T1, T2, T3, T4, T5, T6>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
    where T6 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components, default(T4).Components, default(T5).Components, default(T6).Components);

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public IdentityEnumerator Identities() => new(_api, Components);

    /// Runs a body over every entity, walking identities so it may change the world.
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
