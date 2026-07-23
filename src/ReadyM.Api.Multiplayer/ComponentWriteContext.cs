using System;

namespace ReadyM.Api.Multiplayer;

/// <summary>
/// Thread-local switch that makes plain setter writes on networked components additionally set the
/// API (authoritative) flag. Server-side mod code runs inside a "server authoring" scope (mod ECS
/// system ticks and server RPC handlers), so any write a mod makes to a player-owned component is
/// treated as an authoritative override and replicated back to that owner, with no explicit
/// MarkChangedFromApi() call needed.
///
/// The scope is entered in the mod runtime (where the setter actually executes), so it works across
/// the AOT/mod boundary. It stays off everywhere else: on clients, and for the server framework's own
/// bookkeeping writes (entity creation/archetype setup, delta/snapshot application, scope management),
/// which run outside any authoring scope and must not be mistaken for gameplay overrides.
/// </summary>
public static class ComponentWriteContext
{
    [ThreadStatic]
    private static bool _autoMarkApiOnWrite;

    /// <summary>True when setter writes on the current thread should auto-set the API flag.</summary>
    public static bool AutoMarkApiOnWrite => _autoMarkApiOnWrite;

    /// <summary>
    /// Marks the current thread as executing server game logic for the duration of the returned
    /// scope: setter writes to networked components auto-set the API flag. Restores the previous
    /// value on dispose, so nested scopes and reentrancy behave correctly.
    /// </summary>
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
