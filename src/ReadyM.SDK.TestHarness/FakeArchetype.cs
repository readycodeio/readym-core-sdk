using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;

namespace ReadyM.SDK.TestHarness;

/// <summary>
/// Entities that carry the same component set, with one heap per component.
/// </summary>
/// <remarks>
/// The entity array is pinned because a chunk callback hands out its address, and the ABI says that
/// address stays good for as long as the mod side holds it.
/// </remarks>
internal sealed class FakeArchetype(int[] componentIds, FakeHeap[] heaps)
{
    internal int[] ComponentIds { get; } = componentIds;

    internal RawEntity[] Entities { get; private set; } = GC.AllocateArray<RawEntity>(8, pinned: true);

    internal int Count { get; private set; }

    internal int AppendRow()
    {
        if (Count == Entities.Length)
        {
            var bigger = GC.AllocateArray<RawEntity>(Entities.Length * 2, pinned: true);
            Entities.AsSpan(0, Count).CopyTo(bigger);
            Entities = bigger;
        }

        foreach (var heap in heaps)
            heap.Add();

        return Count++;
    }

    /// <summary>Removes a row by moving the last one into it, and says which entity moved.</summary>
    internal RawEntity RemoveRow(int row)
    {
        var last = --Count;

        if (row == last)
            return default;

        foreach (var heap in heaps)
            heap.Move(last, row);

        var moved = Entities[last];
        Entities[row] = moved;

        return moved;
    }

    internal FakeHeap HeapOf(int componentId)
        => HeapOrNull(componentId) ?? throw new InvalidOperationException($"No component {componentId} here.");

    internal FakeHeap? HeapOrNull(int componentId)
    {
        var at = Array.IndexOf(ComponentIds, componentId);
        return at < 0 ? null : heaps[at];
    }

    /// Writes one chunk slot per requested component, or reports that this archetype does not match.
    internal unsafe bool Fill(int[] wanted, ChunkComponent* comps)
    {
        for (var i = 0; i < wanted.Length; i++)
        {
            var at = Array.IndexOf(ComponentIds, wanted[i]);

            if (at < 0)
                return false;

            comps[i] = heaps[at].Chunk();
        }

        return true;
    }
}
