using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Server.Entity;

/// 2 shapes walked by identity, which is the fallback when any of them has no chunk view.
public readonly ref struct QueryBuilder<T1, T2>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;

    // Combined once rather than per query, for the same reason the chunk views cache theirs.
    private static readonly ComponentSet Combined = ComponentSet.Combine(default(T1).Components, default(T2).Components);

    internal QueryBuilder(ServerEntityApi api) => _api = api;

    internal Enumerator GetEnumerator() => new(_api, _api.CollectMatching(Combined));

    public ref struct Enumerator
    {
        private readonly ServerEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal Enumerator(ServerEntityApi api, EntityBuffer buffer)
        {
            _api = api;
            _buffer = buffer;
            _index = -1;
        }

        public readonly (T1, T2) Current => (
            new T1 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T2 { Handle = new EntityHandle(_buffer[_index], _api) });

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose() => _buffer.Return();
    }
}
/// 3 shapes walked by identity, which is the fallback when any of them has no chunk view.
public readonly ref struct QueryBuilder<T1, T2, T3>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeQueryable
    where T3 : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;

    // Combined once rather than per query, for the same reason the chunk views cache theirs.
    private static readonly ComponentSet Combined = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components);

    internal QueryBuilder(ServerEntityApi api) => _api = api;

    internal Enumerator GetEnumerator() => new(_api, _api.CollectMatching(Combined));

    public ref struct Enumerator
    {
        private readonly ServerEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal Enumerator(ServerEntityApi api, EntityBuffer buffer)
        {
            _api = api;
            _buffer = buffer;
            _index = -1;
        }

        public readonly (T1, T2, T3) Current => (
            new T1 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T2 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T3 { Handle = new EntityHandle(_buffer[_index], _api) });

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose() => _buffer.Return();
    }
}
/// 4 shapes walked by identity, which is the fallback when any of them has no chunk view.
public readonly ref struct QueryBuilder<T1, T2, T3, T4>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeQueryable
    where T3 : struct, IArchetypeQueryable
    where T4 : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;

    // Combined once rather than per query, for the same reason the chunk views cache theirs.
    private static readonly ComponentSet Combined = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components, default(T4).Components);

    internal QueryBuilder(ServerEntityApi api) => _api = api;

    internal Enumerator GetEnumerator() => new(_api, _api.CollectMatching(Combined));

    public ref struct Enumerator
    {
        private readonly ServerEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal Enumerator(ServerEntityApi api, EntityBuffer buffer)
        {
            _api = api;
            _buffer = buffer;
            _index = -1;
        }

        public readonly (T1, T2, T3, T4) Current => (
            new T1 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T2 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T3 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T4 { Handle = new EntityHandle(_buffer[_index], _api) });

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose() => _buffer.Return();
    }
}
/// 5 shapes walked by identity, which is the fallback when any of them has no chunk view.
public readonly ref struct QueryBuilder<T1, T2, T3, T4, T5>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeQueryable
    where T3 : struct, IArchetypeQueryable
    where T4 : struct, IArchetypeQueryable
    where T5 : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;

    // Combined once rather than per query, for the same reason the chunk views cache theirs.
    private static readonly ComponentSet Combined = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components, default(T4).Components, default(T5).Components);

    internal QueryBuilder(ServerEntityApi api) => _api = api;

    internal Enumerator GetEnumerator() => new(_api, _api.CollectMatching(Combined));

    public ref struct Enumerator
    {
        private readonly ServerEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal Enumerator(ServerEntityApi api, EntityBuffer buffer)
        {
            _api = api;
            _buffer = buffer;
            _index = -1;
        }

        public readonly (T1, T2, T3, T4, T5) Current => (
            new T1 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T2 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T3 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T4 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T5 { Handle = new EntityHandle(_buffer[_index], _api) });

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose() => _buffer.Return();
    }
}
/// 6 shapes walked by identity, which is the fallback when any of them has no chunk view.
public readonly ref struct QueryBuilder<T1, T2, T3, T4, T5, T6>
    where T1 : struct, IArchetypeQueryable
    where T2 : struct, IArchetypeQueryable
    where T3 : struct, IArchetypeQueryable
    where T4 : struct, IArchetypeQueryable
    where T5 : struct, IArchetypeQueryable
    where T6 : struct, IArchetypeQueryable
{
    private readonly ServerEntityApi _api;

    // Combined once rather than per query, for the same reason the chunk views cache theirs.
    private static readonly ComponentSet Combined = ComponentSet.Combine(default(T1).Components, default(T2).Components, default(T3).Components, default(T4).Components, default(T5).Components, default(T6).Components);

    internal QueryBuilder(ServerEntityApi api) => _api = api;

    internal Enumerator GetEnumerator() => new(_api, _api.CollectMatching(Combined));

    public ref struct Enumerator
    {
        private readonly ServerEntityApi _api;
        private readonly EntityBuffer _buffer;
        private int _index;

        internal Enumerator(ServerEntityApi api, EntityBuffer buffer)
        {
            _api = api;
            _buffer = buffer;
            _index = -1;
        }

        public readonly (T1, T2, T3, T4, T5, T6) Current => (
            new T1 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T2 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T3 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T4 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T5 { Handle = new EntityHandle(_buffer[_index], _api) },
            new T6 { Handle = new EntityHandle(_buffer[_index], _api) });

        public bool MoveNext() => ++_index < _buffer.Count;

        public readonly void Dispose() => _buffer.Return();
    }
}
