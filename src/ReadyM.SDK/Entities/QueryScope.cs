using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;
using ReadyM.SDK.Exceptions;

namespace ReadyM.SDK.Entities;

/// Collects entity deletions during a query, so they can be deferred to after the query ends.
/// <remarks>
/// Deleting entities inside a query works, with an attempt to reference a deleted entity throwing.
/// Creating new entities is forbidden, since it may resize the underlying array, thus invalidating chunk pointers.
/// </remarks>
internal struct QueryScope()
{
    private int _count;
    private int _depth;

    internal HashSet<RawEntity> Pending { get; } = [];
    internal readonly bool InQuery => _depth > 0;

    /// <summary>The count is the whole fast path; the lookup is kept out of line behind it.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly bool IsPending(RawEntity rawEntity) => _count > 0 && Contains(rawEntity);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private readonly bool Contains(RawEntity rawEntity) => Pending.Contains(rawEntity);

    internal void Enter() => _depth++;

    /// True when the outermost query just ended with deletes to apply.
    internal bool Leave() => --_depth == 0 && _count > 0;

    /// False when the entity was already asked for.
    internal bool Mark(RawEntity rawEntity)
    {
        if (!Pending.Add(rawEntity))
            return false;

        _count++;
        return true;
    }

    internal void Clear()
    {
        Pending.Clear();
        _count = 0;
    }

    internal readonly void RefuseIfInQuery(string what)
    {
        if (_depth > 0)
            throw new StructuralChangeInQueryException(what);
    }
}