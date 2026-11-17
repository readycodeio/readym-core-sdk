using ReadyM.Api.Multiplayer.Interop;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Chunks;

internal interface IChunkSource
{
    /// <param name="filter">The dimensions a server query sees; a client sees one world and ignores it.</param>
    ChunkBuffer Collect(ComponentSet components, DimensionFilter filter);

    EntityHandle Prototype { get; }

    void EnterQuery();

    void LeaveQuery();
}