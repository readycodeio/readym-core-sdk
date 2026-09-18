using System.ComponentModel;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Chunks;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server.Entities;

/// Every entity carrying all 2 shapes, without an archetype that names them.
public readonly ref struct EntityQuery<T1, T2>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;

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

    /// Narrows the query to the entities one scope holds.
    public ScopedQuery<T1, T2> InScope(Scope scope) => new(_api, scope);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct IdentityEnumerator : IDisposable
    {
        private readonly ServerEntityApi? _api;
        private readonly EntityBuffer? _buffer;
        private int _index;

        internal IdentityEnumerator(ServerEntityApi? api, ComponentSet components)
        {
            // A query nobody gave a world to matches nothing, so a property can hand one out
            // when there is nothing to look in.
            if (api is null)
            {
                _api = null;
                _buffer = null;
                _index = -1;
                return;
            }

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
                var handle = new EntityHandle(_buffer![_index], _api!);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle });
            }
        }

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

/// Every entity carrying all 3 shapes, without an archetype that names them.
public readonly ref struct EntityQuery<T1, T2, T3>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;

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

    /// Narrows the query to the entities one scope holds.
    public ScopedQuery<T1, T2, T3> InScope(Scope scope) => new(_api, scope);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct IdentityEnumerator : IDisposable
    {
        private readonly ServerEntityApi? _api;
        private readonly EntityBuffer? _buffer;
        private int _index;

        internal IdentityEnumerator(ServerEntityApi? api, ComponentSet components)
        {
            // A query nobody gave a world to matches nothing, so a property can hand one out
            // when there is nothing to look in.
            if (api is null)
            {
                _api = null;
                _buffer = null;
                _index = -1;
                return;
            }

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
                var handle = new EntityHandle(_buffer![_index], _api!);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle },
                    new T3 { Handle = handle });
            }
        }

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

/// Every entity carrying all 4 shapes, without an archetype that names them.
public readonly ref struct EntityQuery<T1, T2, T3, T4>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;

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

    /// Narrows the query to the entities one scope holds.
    public ScopedQuery<T1, T2, T3, T4> InScope(Scope scope) => new(_api, scope);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct IdentityEnumerator : IDisposable
    {
        private readonly ServerEntityApi? _api;
        private readonly EntityBuffer? _buffer;
        private int _index;

        internal IdentityEnumerator(ServerEntityApi? api, ComponentSet components)
        {
            // A query nobody gave a world to matches nothing, so a property can hand one out
            // when there is nothing to look in.
            if (api is null)
            {
                _api = null;
                _buffer = null;
                _index = -1;
                return;
            }

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
                var handle = new EntityHandle(_buffer![_index], _api!);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle },
                    new T3 { Handle = handle },
                    new T4 { Handle = handle });
            }
        }

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

/// Every entity carrying all 5 shapes, without an archetype that names them.
public readonly ref struct EntityQuery<T1, T2, T3, T4, T5>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;

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

    /// Narrows the query to the entities one scope holds.
    public ScopedQuery<T1, T2, T3, T4, T5> InScope(Scope scope) => new(_api, scope);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct IdentityEnumerator : IDisposable
    {
        private readonly ServerEntityApi? _api;
        private readonly EntityBuffer? _buffer;
        private int _index;

        internal IdentityEnumerator(ServerEntityApi? api, ComponentSet components)
        {
            // A query nobody gave a world to matches nothing, so a property can hand one out
            // when there is nothing to look in.
            if (api is null)
            {
                _api = null;
                _buffer = null;
                _index = -1;
                return;
            }

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
                var handle = new EntityHandle(_buffer![_index], _api!);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle },
                    new T3 { Handle = handle },
                    new T4 { Handle = handle },
                    new T5 { Handle = handle });
            }
        }

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

/// Every entity carrying all 6 shapes, without an archetype that names them.
public readonly ref struct EntityQuery<T1, T2, T3, T4, T5, T6>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
    where T6 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;

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

    /// Narrows the query to the entities one scope holds.
    public ScopedQuery<T1, T2, T3, T4, T5, T6> InScope(Scope scope) => new(_api, scope);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct IdentityEnumerator : IDisposable
    {
        private readonly ServerEntityApi? _api;
        private readonly EntityBuffer? _buffer;
        private int _index;

        internal IdentityEnumerator(ServerEntityApi? api, ComponentSet components)
        {
            // A query nobody gave a world to matches nothing, so a property can hand one out
            // when there is nothing to look in.
            if (api is null)
            {
                _api = null;
                _buffer = null;
                _index = -1;
                return;
            }

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
                var handle = new EntityHandle(_buffer![_index], _api!);

                return (
                    new T1 { Handle = handle },
                    new T2 { Handle = handle },
                    new T3 { Handle = handle },
                    new T4 { Handle = handle },
                    new T5 { Handle = handle },
                    new T6 { Handle = handle });
            }
        }

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
