using System.ComponentModel;
using ReadyM.Api.Idents;

namespace ReadyM.SDK.Events;

/// <summary>What a networked event handler is told about the event besides its members.</summary>
public readonly struct EventContext
{
    internal EventContext(PlayerId origin, bool isRaisingClient, EventCancellation cancellation)
    {
        Origin = origin;
        IsRaisingClient = isRaisingClient;
        Cancellation = cancellation;
    }

    /// <summary>Who raised it: a player, or <see cref="PlayerId.Server"/>.</summary>
    public PlayerId Origin { get; }

    /// <exclude />
    /// <summary>Whether this machine is the client that raised the event. Read by <c>SkipOnRaisingClient</c>.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsRaisingClient { get; }

    /// <summary>On <c>Stage.Rejected</c>, the unique ID of the mod that cancelled the event.</summary>
    public string CancelledBy => Cancellation.CancelledBy;

    /// <exclude />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsCancelled => Cancellation.IsCancelled;

    /// <exclude />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public object? CancellationReason => Cancellation.Reason;

    internal EventCancellation Cancellation { get; }

    /// <summary>
    /// Cancels the event, with a reason of the type its declaration named. Every later stage is skipped and the
    /// reason reaches the origin.
    /// </summary>
    public void Cancel<TReason>(TReason reason) => Cancellation.Cancel(reason);

    /// <summary>Cancels the event without a reason. Every later stage is skipped.</summary>
    public void Cancel() => Cancellation.Cancel(null);
}
