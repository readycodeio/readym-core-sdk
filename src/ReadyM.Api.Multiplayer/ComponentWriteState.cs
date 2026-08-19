using ReadyM.Api.Multiplayer.ConflictResolution;

namespace ReadyM.Api.Multiplayer;

public struct ComponentWriteState(
    bool autoMarkApiOnWrite,
    uint currentTime,
    uint lastObservedTime,
    IChangeTrackingStore? conflictResolver)
{
    public readonly bool AutoMarkApiOnWrite = autoMarkApiOnWrite;
    public readonly uint CurrentTime = currentTime;
    public readonly uint LastObservedTime = lastObservedTime;
    public readonly IChangeTrackingStore? ConflictResolver = conflictResolver;
}
