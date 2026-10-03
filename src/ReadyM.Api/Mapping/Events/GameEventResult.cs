using System;

namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// What a machine does with a game event. Prescriptive: it says which effects to run, not why.
/// </summary>
[Flags]
public enum GameEventResult : byte
{
    DontRun = 0,
    RunOptimistic = 1 << 0,
    RunAuthoritative = 1 << 1,
    UndoOptimistic = 1 << 2,

    /// <summary>This machine may not run the event at all. Never combined with a run flag.</summary>
    Rejected = 1 << 7,

    RunAll = RunOptimistic | RunAuthoritative,
}

public static class GameEventResultExtensions
{
    public static bool Runs(this GameEventResult result)
        => (result & GameEventResult.RunAll) != 0;

    public static bool IsRejected(this GameEventResult result)
        => (result & GameEventResult.Rejected) != 0;
}
