using System;

namespace ReadyM.Api.Multiplayer;

/// <exclude />
/// <summary>
/// Thread-local switch that makes plain setter writes on networked components also set the API
/// (authoritative) flag. Server mod code runs inside a "server authoring" scope (mod system ticks
/// and RPC handlers), so its writes to player-owned components override the owner without an explicit
/// MarkChangedFromApi() call. Entered in the mod runtime (where the setter runs) so it crosses the
/// AOT/mod boundary; off everywhere else, so framework bookkeeping writes aren't mistaken for overrides.
/// </summary>
public static class ComponentWriteContext
{
    /// <summary>True when setter writes on the current thread should auto-set the API flag.</summary>
    [field: ThreadStatic]
    public static ComponentWriteState Current { get; private set; }

    /// <summary>Enables auto-marking for the scope (restored on dispose).</summary>
    internal static Scope EnterServerAuthoring()
    {
        if (Current.CurrentTime == 0)
            throw new InvalidOperationException("Cannot enter server authoring without entering server-side first");

        var previous = Current;
        Current = new ComponentWriteState(true, Current.CurrentTime, Current.CurrentTime, true);
        return new Scope(previous);
    }

    internal static Scope EnterServerSide(uint currentTime)
    {
        if (currentTime == 0)
            throw new ArgumentOutOfRangeException(nameof(currentTime), "Current time must be non-zero");
        if (Current.CurrentTime > 0)
            throw new InvalidOperationException("Already in server-side context");

        var previous = Current;
        Current = new ComponentWriteState(Current.AutoMarkApiOnWrite, currentTime, Current.LastObservedTime, Current.ResolveConflicts);
        return new Scope(previous);
    }

    internal static Scope EnterClientApply(uint lastObserved)
    {
        if (lastObserved == 0)
            throw new ArgumentOutOfRangeException(nameof(lastObserved), "Last observed time must be non-zero");
        if (Current.CurrentTime == 0)
            throw new InvalidOperationException("Cannot enter client apply without entering server-side first");
        if (lastObserved > Current.CurrentTime)
            throw new ArgumentOutOfRangeException(nameof(lastObserved), "Last observed time cannot be greater than current time");

        var previous = Current;
        Current = new ComponentWriteState(Current.AutoMarkApiOnWrite, Current.CurrentTime, lastObserved, true);
        return new Scope(previous);
    }

    internal static Scope EnterServerApply()
    {
        if (Current.CurrentTime == 0)
            throw new InvalidOperationException("Cannot enter server apply without entering server-side first");

        var previous = Current;
        Current = new ComponentWriteState(Current.AutoMarkApiOnWrite, Current.CurrentTime, Current.CurrentTime, true);
        return new Scope(previous);
    }

    internal struct Scope : IDisposable
    {
        private readonly ComponentWriteState _previous;
        private bool _disposed;

        internal Scope(ComponentWriteState previous)
        {
            _previous = previous;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Current = _previous;
        }
    }
}
