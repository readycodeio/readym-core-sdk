using System.ComponentModel;
using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Chunks;

/// <summary>
/// The chunk-backed twin of an archetype or mixin: the same properties, reached straight off the
/// chunk rather than by asking where the entity lives on every access.
/// </summary>
/// <remarks>
/// A query builds one of these per chunk and then only moves its index, so the work of finding each
/// component happens once per chunk instead of once per read. It is a ref struct, so it cannot
/// outlive the loop that produced it.
///
/// Generated code in a mod assembly implements this, and that mod keeps running against later SDKs,
/// so a member added here without a body would stop every already-compiled mod from loading.
/// Anything added later must have one, static members included.
/// </remarks>
public interface IArchetypeChunkView<out TSelf> where TSelf : IArchetypeChunkView<TSelf>, allows ref struct
{
    /// <summary>How many slots this view consumes, so several can share one chunk's worth.</summary>
    /// <remarks>
    /// One slot per component, which the set already knows, so a view that does not say is not
    /// wrong. The generator states it anyway, as a constant rather than a field read.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    static virtual int SlotCount => TSelf.Components.Count;

    /// The components a query has to ask for, in the order the slots arrive in.
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract ComponentSet Components { get; }

    /// <summary>Binds the view to one chunk, positioned at its first entity.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    static abstract TSelf Bind(
        ReadOnlySpan<ChunkSlot> slots,
        ReadOnlySpan<RawEntity> entities,
        EntityHandle prototype);

    /// <summary>
    /// The same binding, moved to another entity of the same chunk.
    /// </summary>
    /// <remarks>
    /// Only the index moves. The view keeps a reference to the chunk's identities rather than one
    /// identity, so a loop that never asks for a handle never pays to produce one, which measured as
    /// the whole of v1's deficit against v0 on this path.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    TSelf At(int index);
}
