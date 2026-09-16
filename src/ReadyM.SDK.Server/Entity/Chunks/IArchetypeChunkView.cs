using System.ComponentModel;
using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Server.Entity.Chunks;

/// <summary>
/// The chunk-backed twin of an archetype or mixin: the same properties, reached straight off the
/// chunk rather than by asking where the entity lives on every access.
/// </summary>
/// <remarks>
/// A query builds one of these per chunk and then only moves its index, so the work of finding each
/// component happens once per chunk instead of once per read. It is a ref struct, so it cannot
/// outlive the loop that produced it.
/// </remarks>
public interface IArchetypeChunkView<out TSelf> where TSelf : IArchetypeChunkView<TSelf>, allows ref struct
{
    /// <summary>How many slots this view consumes, so several can share one chunk's worth.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract int SlotCount { get; }

    /// The components a query has to ask for, in the order the slots arrive in.
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract ComponentSet Components { get; }

    /// <summary>Binds the view to one chunk, positioned at its first entity.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract TSelf Bind(
        ReadOnlySpan<ChunkSlot> slots,
        ReadOnlySpan<RawEntity> entities,
        EntityHandle prototype);

    /// <summary>The same binding, moved to another entity of the same chunk.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    TSelf At(int index);
}
