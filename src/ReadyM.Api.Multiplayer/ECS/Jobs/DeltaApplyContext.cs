using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

/// <summary>
/// Thread-local side channel carrying the authoritative sender of the delta batch currently being
/// applied. The server sets it around <see cref="SerializationJobRegistry.ApplyDelta"/> so that
/// <see cref="ApplyDeltaJob{T}"/> can reject deltas whose sender does not own the target entity
/// (a client may only change components on entities it owns).
///
/// It is left unset on the client: deltas there arrive from the trusted server relay, which has
/// already validated ownership, so no per-sender check runs.
/// </summary>
internal static class DeltaApplyContext
{
    [ThreadStatic]
    private static PlayerId? _authoritativeSender;

    /// <summary>The owner a delta's entities must match, or null when no check should run.</summary>
    public static PlayerId? AuthoritativeSender => _authoritativeSender;

    /// <summary>
    /// Sets the authoritative sender for the duration of the returned scope, restoring the previous
    /// value on dispose. Must be entered on the same thread that runs the apply job.
    /// </summary>
    public static Scope WithAuthoritativeSender(PlayerId sender)
    {
        var previous = _authoritativeSender;
        _authoritativeSender = sender;
        return new Scope(previous);
    }

    public struct Scope : IDisposable
    {
        private readonly PlayerId? _previous;
        private bool _disposed;

        internal Scope(PlayerId? previous)
        {
            _previous = previous;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _authoritativeSender = _previous;
        }
    }
}
