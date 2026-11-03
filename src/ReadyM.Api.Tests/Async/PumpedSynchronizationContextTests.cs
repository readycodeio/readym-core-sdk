using Microsoft.Extensions.Logging;
using ReadyM.Api.Async;

namespace ReadyM.Api.Tests.Async;

// The safety net for the correctness rules a single-threaded SynchronizationContext is famous for getting
// wrong. The "pump thread" is the test thread; we assert continuations run on it (never the thread pool) by
// capturing Environment.CurrentManagedThreadId inside them. Happy-path tests use FailOnErrorLogger so an
// unexpected error log turns the test red.
public class PumpedSynchronizationContextTests
{
    private static PumpedSynchronizationContext NewContext() => new(new FailOnErrorLogger());

    // Pump until the task finishes (or a generous cap), so pool-completed awaits get a chance to post back.
    private static void PumpUntilComplete(PumpedSynchronizationContext ctx, Task task)
    {
        for (int i = 0; i < 10_000 && !task.IsCompleted; i++)
        {
            ctx.Pump();
            if (!task.IsCompleted) Thread.Sleep(1);
        }
    }

    [Fact]
    public void ContinuationAfterYieldRunsOnThePumpThread()
    {
        int pumpThreadId = Environment.CurrentManagedThreadId;
        int? continuationThreadId = null;
        var ctx = NewContext();

        Task task = null!;
        ctx.Start(() => task = Body());
        async Task Body()
        {
            await ctx.Yield();
            continuationThreadId = Environment.CurrentManagedThreadId;
        }

        Assert.False(task.IsCompleted);          // suspended at Yield
        Assert.Equal(1, ctx.Pump());             // the continuation runs on this (pump) thread
        Assert.True(task.IsCompleted);
        Assert.Equal(pumpThreadId, continuationThreadId);
    }

    [Fact]
    public void PostAwaitBodyRunsOnPumpThreadEvenWhenAwaitedTaskCompletedOnThePool()
    {
        int pumpThreadId = Environment.CurrentManagedThreadId;
        int? afterPoolThreadId = null;
        int poolThreadId = -1;
        var ctx = NewContext();

        Task task = null!;
        ctx.Start(() => task = Body());
        async Task Body()
        {
            await Task.Run(() => poolThreadId = Environment.CurrentManagedThreadId); // completes on a pool thread
            afterPoolThreadId = Environment.CurrentManagedThreadId;                  // must be the pump thread
        }

        PumpUntilComplete(ctx, task);

        Assert.True(task.IsCompleted, "task did not complete");
        Assert.NotEqual(pumpThreadId, poolThreadId);          // the awaited work really did run off-thread
        Assert.Equal(pumpThreadId, afterPoolThreadId);        // but the continuation came back to the pump thread
    }

    [Fact]
    public void PumpDrainsOnlyWorkQueuedSoFar()
    {
        var ctx = NewContext();
        var log = new List<int>();

        ctx.Post(_ => { log.Add(1); ctx.Post(__ => log.Add(3), null); }, null); // queues more work while running
        ctx.Post(_ => log.Add(2), null);

        int processed = ctx.Pump();
        Assert.Equal(2, processed);                 // only the two queued as of entry
        Assert.Equal(new[] { 1, 2 }, log);          // the re-queued item did NOT run this pump
        Assert.True(ctx.HasPendingWork);

        ctx.Pump();
        Assert.Equal(new[] { 1, 2, 3 }, log);       // it runs on the next pump, FIFO
    }

    [Fact]
    public void EntireMultiAwaitMethodStaysOnThePumpThread()
    {
        int pumpThreadId = Environment.CurrentManagedThreadId;
        var observed = new List<int>();
        var ctx = NewContext();

        Task task = null!;
        ctx.Start(() => task = Body());
        async Task Body()
        {
            observed.Add(Environment.CurrentManagedThreadId);           // before first await
            await ctx.Yield();
            observed.Add(Environment.CurrentManagedThreadId);
            await Task.Run(() => { });                                  // pool-completed await
            observed.Add(Environment.CurrentManagedThreadId);
            await ctx.WaitPumps(2);
            observed.Add(Environment.CurrentManagedThreadId);
        }

        PumpUntilComplete(ctx, task);

        Assert.True(task.IsCompleted, "task did not complete");
        Assert.Equal(4, observed.Count);
        Assert.All(observed, id => Assert.Equal(pumpThreadId, id));     // never a non-pump thread
    }

    [Fact]
    public void CancellingCompletesSuspendedYieldWithCancellationAndRunsFinallyOnPumpThread()
    {
        int pumpThreadId = Environment.CurrentManagedThreadId;
        var cts = new CancellationTokenSource();
        bool finallyRan = false;
        int? finallyThreadId = null;
        var ctx = NewContext();

        Task task = null!;
        ctx.Start(() => task = Body());
        async Task Body()
        {
            try
            {
                while (true)
                    await ctx.Yield(cts.Token);
            }
            finally
            {
                finallyRan = true;
                finallyThreadId = Environment.CurrentManagedThreadId;
            }
        }

        ctx.Pump();          // one loop iteration; re-suspends at Yield
        Assert.False(task.IsCompleted);

        cts.Cancel();
        ctx.Pump();          // resume -> GetResult throws OperationCanceledException -> finally runs

        Assert.True(task.IsCompleted);
        Assert.True(finallyRan);
        Assert.Equal(pumpThreadId, finallyThreadId);
        Assert.True(task.IsCanceled);
    }

    [Fact]
    public void FaultingContinuationIsLoggedAndTheRestStillRun()
    {
        var logger = new CapturingLogger();
        var ctx = new PumpedSynchronizationContext(logger);
        bool secondRan = false;

        ctx.Post(_ => throw new InvalidOperationException("boom"), null);
        ctx.Post(_ => secondRan = true, null);

        int processed = ctx.Pump();

        Assert.Equal(2, processed);
        Assert.True(secondRan);                                        // one bad continuation does not stop the rest
        var error = Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.IsType<InvalidOperationException>(error.Exception);     // logged, not swallowed
    }

    [Fact]
    public void EmptyPumpDoesNothingAndReportsNoWork()
    {
        var ctx = NewContext();
        Assert.False(ctx.HasPendingWork);
        Assert.Equal(0, ctx.Pump());
    }

    [Fact]
    public void NullLoggerIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new PumpedSynchronizationContext(null!));
    }
}
