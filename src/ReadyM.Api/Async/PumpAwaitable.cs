using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ReadyM.Api.Async;

/// <summary>Awaitable returned by <see cref="PumpedSynchronizationContext.Yield"/>. Awaiting it yields until
/// the next pump and resumes on the pump thread.</summary>
internal readonly struct PumpAwaitable
{
    private readonly PumpedSynchronizationContext _context;
    private readonly CancellationToken _cancellationToken;

    internal PumpAwaitable(PumpedSynchronizationContext context, CancellationToken cancellationToken)
    {
        _context = context;
        _cancellationToken = cancellationToken;
    }

    public PumpAwaiter GetAwaiter() => new(_context, _cancellationToken);
}

/// <summary>Awaiter for <see cref="PumpAwaitable"/>. Always suspends exactly once and resumes the continuation
/// on the next pump, directly on the pump thread. Resumption is scheduled straight onto the context (not via
/// the ambient <see cref="SynchronizationContext"/>), so a method that only awaits
/// <see cref="PumpedSynchronizationContext.Yield"/> is pump-thread-bound regardless of any
/// SynchronizationContext-capture subtlety.</summary>
internal readonly struct PumpAwaiter : ICriticalNotifyCompletion
{
    // Cached so scheduling a continuation allocates nothing beyond the queued work item.
    private static readonly SendOrPostCallback InvokeContinuation = static state => ((Action)state!)();

    private readonly PumpedSynchronizationContext _context;
    private readonly CancellationToken _cancellationToken;

    internal PumpAwaiter(PumpedSynchronizationContext context, CancellationToken cancellationToken)
    {
        _context = context;
        _cancellationToken = cancellationToken;
    }

    // Always yield: never complete synchronously.
    public bool IsCompleted => false;

    public void OnCompleted(Action continuation) => _context.Post(InvokeContinuation, continuation);

    public void UnsafeOnCompleted(Action continuation) => _context.Post(InvokeContinuation, continuation);

    // Re-checked on every resume, so a loop awaiting Yield() ends promptly (on the pump thread) once the token
    // trips.
    public void GetResult() => _cancellationToken.ThrowIfCancellationRequested();
}
