namespace ReadyM.Api.Multiplayer;

public struct ComponentWriteState(
    bool autoMarkApiOnWrite,
    uint currentTime,
    uint lastObservedTime,
    bool resolveConflicts)
{
    public readonly bool AutoMarkApiOnWrite = autoMarkApiOnWrite;
    public readonly uint CurrentTime = currentTime;
    public readonly uint LastObservedTime = lastObservedTime;
    public readonly bool ResolveConflicts = resolveConflicts;
}
