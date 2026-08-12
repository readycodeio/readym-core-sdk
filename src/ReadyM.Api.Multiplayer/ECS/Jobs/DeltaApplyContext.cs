using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Jobs;

/// <summary>
/// Thread-local sender of the delta batch being applied. The server sets it around
/// <see cref="SerializationJobRegistry.ApplyDelta"/> so <see cref="ApplyDeltaJob{T}"/> can reject
/// deltas from a non-owner. Left unset on the client (deltas there are trusted server relays).
/// </summary>
internal static class DeltaApplyContext
{
    [ThreadStatic]
    private static PlayerId? _authoritativeSender;

    /// <summary>The owner a delta's entities must match, or null to skip the check.</summary>
    public static PlayerId? AuthoritativeSender => _authoritativeSender;

    /// <summary>Sets the sender for the scope (restored on dispose). Enter on the apply thread.</summary>
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
