using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Async;

/// <summary>
/// A single-threaded <see cref="SynchronizationContext"/> whose queued continuations run only when the owner
/// calls <see cref="Pump"/>, on the pumping thread. This lets <c>async</c>/<c>await</c> code run entirely on a
/// caller-owned thread that is advanced cooperatively, instead of on the thread pool. It is thread-agnostic:
/// the "pump thread" is simply whichever thread calls <see cref="Pump"/> (a game loop, a worker, a test), and
/// an application may create several independent contexts for different purposes.
///
/// Grounded in well-documented references (single-threaded SynchronizationContexts are famously easy to get
/// wrong):
///   - Stephen Toub, "Await, SynchronizationContext, and Console Apps" (the canonical single-threaded pump).
///   - Microsoft.VisualStudio.Threading's SingleThreadedSynchronizationContext.
///   - The System.Threading.SynchronizationContext docs.
/// Unlike Toub's blocking RunOnCurrentThread loop, <see cref="Pump"/> is NON-blocking: it drains the work
/// queued so far and returns, so the host stays in control of its loop.
/// </summary>
internal sealed class PumpedSynchronizationContext(ILogger logger) : SynchronizationContext
{
    private readonly struct WorkItem(SendOrPostCallback callback, object? state)
    {
        public readonly SendOrPostCallback Callback = callback;
        public readonly object? State = state;
    }

    private readonly object _gate = new();
    private readonly Queue<WorkItem> _queue = new();  // reuses its backing array across pumps (low-allocation)
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private int _pumpThreadId;  // 0 until the first Start/Pump; the thread continuations are guaranteed to run on

    /// <summary>The managed thread id continuations run on (0 until the first <see cref="Start"/>/<see cref="Pump"/>).</summary>
    public int PumpThreadId => Volatile.Read(ref _pumpThreadId);

    /// <summary>Whether any continuations are currently queued.</summary>
    public bool HasPendingWork
    {
        get { lock (_gate) return _queue.Count > 0; }
    }

    /// <inheritdoc/>
    public override void Post(SendOrPostCallback d, object? state)
    {
        if (d is null) throw new ArgumentNullException(nameof(d));
        lock (_gate) _queue.Enqueue(new WorkItem(d, state));
    }

    /// <summary>
    /// Upper bound for how long a cross-thread <see cref="Send"/> waits for the pump to run its callback.
    /// Hitting it means the pump stopped draining; without a bound the caller would block forever.
    /// </summary>
    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public override void Send(SendOrPostCallback d, object? state)
    {
        if (d is null) throw new ArgumentNullException(nameof(d));

        if (Environment.CurrentManagedThreadId == Volatile.Read(ref _pumpThreadId))
        {
            RunWithContextInstalled(d, state);
            return;
        }

        // Cross-thread Send: enqueue and block until the pump runs it, propagating any exception to the
        // caller with its original stack. The wait is bounded because an unbounded one turns a stopped
        // pump into a permanent hang in the caller.
        var completion = new TaskCompletionSource<ExceptionDispatchInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(_ =>
        {
            try
            {
                d(state);
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetResult(ExceptionDispatchInfo.Capture(ex));
            }
        }, null);

        if (!completion.Task.Wait(SendTimeout))
            throw new TimeoutException(
                $"Timed out after {SendTimeout.TotalSeconds:F0}s waiting for pump thread {PumpThreadId} to run a "
                + "cross-thread Send callback. The pump has stopped draining this context.");

        completion.Task.Result?.Throw();
    }

    /// <inheritdoc/>
    public override SynchronizationContext CreateCopy() => this;

    /// <summary>
    /// Runs <paramref name="action"/> on the calling (pump) thread with this context installed as
    /// <see cref="SynchronizationContext.Current"/>. Use it to START an async method so its pre-await body and
    /// its captured continuations bind to this context. Must be called from the thread that will pump.
    /// </summary>
    public void Start(Action action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        BindPumpThread();
        RunWithContextInstalled(static state => ((Action)state!)(), action);
    }

    /// <summary>
    /// Claims the calling thread as the single pumper, or throws if another thread already owns it. The
    /// single-pumper rule is what makes "continuations run on the pump thread" a guarantee rather than a
    /// convention, so a second pumper is an error rather than a silent transfer of ownership.
    /// </summary>
    private void BindPumpThread()
    {
        var current = Environment.CurrentManagedThreadId;
        var previous = Interlocked.CompareExchange(ref _pumpThreadId, current, 0);
        if (previous != 0 && previous != current)
            throw new InvalidOperationException(
                $"This context is already pumped by thread {previous} and must be pumped by exactly one thread; "
                + $"thread {current} tried to pump it too. Create a separate context for that thread.");
    }

    /// <summary>
    /// Drains the continuations queued AS OF entry, on the calling thread, with this context installed as
    /// Current (so nested awaits re-capture it). Work queued DURING the pump waits for the next pump, so a
    /// continuation that re-queues itself cannot starve the caller within a single tick. A continuation that
    /// throws is logged and dropped (the pump keeps draining and stays alive); it is never swallowed silently.
    /// Returns the number of continuations executed.
    /// </summary>
    public int Pump()
    {
        BindPumpThread();

        int budget;
        lock (_gate) budget = _queue.Count;
        if (budget == 0) return 0;

        var previous = Current;
        SetSynchronizationContext(this);
        var processed = 0;
        try
        {
            for (var i = 0; i < budget; i++)
            {
                WorkItem item;
                lock (_gate)
                {
                    if (_queue.Count == 0) break;
                    item = _queue.Dequeue();
                }

                processed++;
                try
                {
                    item.Callback(item.State);
                }
                catch (Exception ex)
                {
                    // Logged and dropped so one bad continuation cannot kill the pump; never swallowed.
                    _logger.LogError(ex, "A continuation queued on the pumped synchronization context threw; dropping it and continuing to pump.");
                }
            }
        }
        finally
        {
            SetSynchronizationContext(previous);
        }

        return processed;
    }

    /// <summary>
    /// Suspends the awaiting async method until the next <see cref="Pump"/>, resuming ON THE PUMP THREAD. If
    /// <paramref name="cancellationToken"/> is cancelled, resumption throws <see cref="OperationCanceledException"/>
    /// (still on the pump thread, so the method's finally/using run there).
    /// </summary>
    public PumpAwaitable Yield(CancellationToken cancellationToken = default) => new(this, cancellationToken);

    /// <summary>Suspends for <paramref name="pumps"/> pump cycles.</summary>
    public async Task WaitPumps(int pumps, CancellationToken cancellationToken = default)
    {
        if (pumps < 0) throw new ArgumentOutOfRangeException(nameof(pumps), pumps, "Must not be negative.");
        for (var i = 0; i < pumps; i++)
            await Yield(cancellationToken);
    }

    private void RunWithContextInstalled(SendOrPostCallback d, object? state)
    {
        var previous = Current;
        SetSynchronizationContext(this);
        try { d(state); }
        finally { SetSynchronizationContext(previous); }
    }
}
