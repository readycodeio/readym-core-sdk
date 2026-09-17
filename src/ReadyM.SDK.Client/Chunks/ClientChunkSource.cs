using System.Collections.Concurrent;
using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Chunks;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Client.Entity;

/// <summary>
/// Finds the chunks a shape matches by walking the store, which is what the client has instead of an
/// interop boundary.
/// </summary>
/// <remarks>
/// Friflo hands over entity ids rather than identities, so the buffer resolves a revision only for
/// entities a loop actually asks about. Ids cannot go stale while a loop runs, since nothing
/// structural may happen during one.
/// </remarks>
internal sealed class ClientChunkSource(EntityStore store, IEntityApi api) : IChunkSource
{
    // A shape's heap positions and its query never change, and a query re-evaluates which archetypes
    // match every time it is walked, so both are worth keeping rather than rebuilding per loop.
    private readonly ConcurrentDictionary<ComponentSet, Shape> _shapes = new();

    EntityHandle IChunkSource.Prototype => new(default, api);

    void IChunkSource.EnterQuery() => api.EnterQuery();

    void IChunkSource.LeaveQuery() => api.LeaveQuery();

    ChunkBuffer IChunkSource.Collect(ComponentSet components)
    {
        var shape = _shapes.GetOrAdd(components, static (set, self) => self.Describe(set), this);
        var buffer = ChunkBuffer.Rent(shape.Indexes.Length);

        buffer.ResolveIdsThrough(store);

        var indexes = shape.Indexes;

        foreach (var archetype in shape.Query.Archetypes)
        {
            if (archetype.EntityCount == 0)
                continue;

            var slots = buffer.Append(archetype.entityIds, archetype.EntityCount);

            for (var i = 0; i < indexes.Length; i++)
            {
                var array = archetype.heapMap[indexes[i]].ComponentArray
                    ?? throw new InvalidOperationException(
                        $"{shape.Types[i].Name} is not stored in this world as a managed array.");

                slots[i] = new ChunkSlot(array, 0);
            }
        }

        return buffer;
    }

    private Shape Describe(ComponentSet components)
    {
        var types = components.Types;
        var schema = EntityStore.GetEntitySchema();
        var indexes = new int[types.Length];

        for (var i = 0; i < types.Length; i++)
            indexes[i] = schema.ComponentTypeByType[types[i]].StructIndex;

        return new Shape(
            types,
            indexes,
            store.Query(new QueryFilter().AllComponents(ClientComponents.Resolve(components))));
    }

    private sealed record Shape(Type[] Types, int[] Indexes, ArchetypeQuery Query);
}
