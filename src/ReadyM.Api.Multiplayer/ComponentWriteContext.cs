using System;

namespace ReadyM.Api.Multiplayer;

/// <summary>
/// Thread-local switch that makes plain setter writes on networked components also set the API
/// (authoritative) flag. Server mod code runs inside a "server authoring" scope (mod system ticks
/// and RPC handlers), so its writes to player-owned components override the owner without an explicit
/// MarkChangedFromApi() call. Entered in the mod runtime (where the setter runs) so it crosses the
/// AOT/mod boundary; off everywhere else, so framework bookkeeping writes aren't mistaken for overrides.
/// </summary>
public static class ComponentWriteContext
{
    [ThreadStatic]
    private static bool _autoMarkApiOnWrite;

    /// <summary>True when setter writes on the current thread should auto-set the API flag.</summary>
    public static bool AutoMarkApiOnWrite => _autoMarkApiOnWrite;

    /// <summary>Enables auto-marking for the scope (restored on dispose).</summary>
    public static Scope EnterServerAuthoring()
    {
        var previous = _autoMarkApiOnWrite;
        _autoMarkApiOnWrite = true;
        return new Scope(previous);
    }

    public struct Scope : IDisposable
    {
        private readonly bool _previous;
        private bool _disposed;

        internal Scope(bool previous)
        {
            _previous = previous;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _autoMarkApiOnWrite = _previous;
        }
    }
}
