using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Chunks;

internal interface IChunkSource
{
    ChunkBuffer Collect(ComponentSet components);

    EntityHandle Prototype { get; }

    void EnterQuery();

    void LeaveQuery();
}