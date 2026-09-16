using System.ComponentModel;
using JetBrains.Annotations;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Server.Entity.Chunks;

namespace ReadyM.SDK.Server.Entity;

/// <summary>2 shapes at once, without an archetype that names them.</summary>
public readonly ref struct EntityQuery<T1, T2>
    where T1 : struct, IArchetypeMixin
    where T2 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public QueryBuilder<T1, T2>.Enumerator Identities() => new QueryBuilder<T1, T2>(_api).GetEnumerator();
}

/// <summary>3 shapes at once, without an archetype that names them.</summary>
public readonly ref struct EntityQuery<T1, T2, T3>
    where T1 : struct, IArchetypeMixin
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public QueryBuilder<T1, T2, T3>.Enumerator Identities() => new QueryBuilder<T1, T2, T3>(_api).GetEnumerator();
}

/// <summary>4 shapes at once, without an archetype that names them.</summary>
public readonly ref struct EntityQuery<T1, T2, T3, T4>
    where T1 : struct, IArchetypeMixin
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public QueryBuilder<T1, T2, T3, T4>.Enumerator Identities() => new QueryBuilder<T1, T2, T3, T4>(_api).GetEnumerator();
}

/// <summary>5 shapes at once, without an archetype that names them.</summary>
public readonly ref struct EntityQuery<T1, T2, T3, T4, T5>
    where T1 : struct, IArchetypeMixin
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public QueryBuilder<T1, T2, T3, T4, T5>.Enumerator Identities() => new QueryBuilder<T1, T2, T3, T4, T5>(_api).GetEnumerator();
}

/// <summary>6 shapes at once, without an archetype that names them.</summary>
public readonly ref struct EntityQuery<T1, T2, T3, T4, T5, T6>
    where T1 : struct, IArchetypeMixin
    where T2 : struct, IArchetypeMixin
    where T3 : struct, IArchetypeMixin
    where T4 : struct, IArchetypeMixin
    where T5 : struct, IArchetypeMixin
    where T6 : struct, IArchetypeMixin
{
    private readonly ServerEntityApi _api;

    internal EntityQuery(ServerEntityApi api) => _api = api;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public ChunkQuery<TView>.Enumerator Chunks<TView>()
        where TView : IArchetypeChunkView<TView>, allows ref struct
        => new ChunkQuery<TView>(_api).GetEnumerator();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MustDisposeResource]
    public QueryBuilder<T1, T2, T3, T4, T5, T6>.Enumerator Identities() => new QueryBuilder<T1, T2, T3, T4, T5, T6>(_api).GetEnumerator();
}