using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Server.Entities;

/// Every entity a scope holds that carries all 2 shapes. Walked by identity: a scope's entities
/// are scattered through the archetypes they belong to, so there are no chunks to hand back.
public readonly ref struct ScopedQuery<T1, T2>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;
    private readonly Scope _scope;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components);

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

            _buffer = api.CollectInScope(scope.Handle.RawEntity, Components);

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

/// Every entity a scope holds that carries all 3 shapes. Walked by identity: a scope's entities
/// are scattered through the archetypes they belong to, so there are no chunks to hand back.
public readonly ref struct ScopedQuery<T1, T2, T3>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;
    private readonly Scope _scope;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components);

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

            _buffer = api.CollectInScope(scope.Handle.RawEntity, Components);

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

/// Every entity a scope holds that carries all 4 shapes. Walked by identity: a scope's entities
/// are scattered through the archetypes they belong to, so there are no chunks to hand back.
public readonly ref struct ScopedQuery<T1, T2, T3, T4>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;
    private readonly Scope _scope;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components, default(T4).Components);

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

            _buffer = api.CollectInScope(scope.Handle.RawEntity, Components);

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

/// Every entity a scope holds that carries all 5 shapes. Walked by identity: a scope's entities
/// are scattered through the archetypes they belong to, so there are no chunks to hand back.
public readonly ref struct ScopedQuery<T1, T2, T3, T4, T5>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;
    private readonly Scope _scope;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components, default(T4).Components, default(T5).Components);

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

            _buffer = api.CollectInScope(scope.Handle.RawEntity, Components);

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

/// Every entity a scope holds that carries all 6 shapes. Walked by identity: a scope's entities
/// are scattered through the archetypes they belong to, so there are no chunks to hand back.
public readonly ref struct ScopedQuery<T1, T2, T3, T4, T5, T6>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
    where T6 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi? _api;
    private readonly Scope _scope;

    // Combined once per instantiation: Combine allocates, and a query must not.
    private static readonly ComponentSet Components = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components, default(T4).Components, default(T5).Components, default(T6).Components);

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

            _buffer = api.CollectInScope(scope.Handle.RawEntity, Components);

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
