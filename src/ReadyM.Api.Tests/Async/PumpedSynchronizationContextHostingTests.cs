using System.Diagnostics;
using ReadyM.Api.Async;

namespace ReadyM.Api.Tests.Async;

// Covers the paths PumpedSynchronizationContextTests does not: pumping from a dedicated long-lived
// thread (the relay server loop's actual shape) rather than the xunit thread, the cross-thread Send
// branch, fault propagation, and the low-allocation claim. Several of these document hazards rather
// than guarantees; those are named ...IsAHazard and assert the current behaviour so a future change
// to it is deliberate.
public class PumpedSynchronizationContextHostingTests
{
    private static PumpedSynchronizationContext NewContext() => new(new FailOnErrorLogger());

    private const int JoinTimeoutMs = 30_000;

    private static Thread StartPumpThread(string name, Action<PumpedSynchronizationContext> body, PumpedSynchronizationContext ctx)
    {
        var thread = new Thread(() => body(ctx)) { IsBackground = true, Name = name };
        thread.Start();
        return thread;
    }

    // The real integration: startup awaits work that completes on the pool (the Agones handshake),
    // then a fixed-rate loop ticks forever. Every observation must land on the one owned thread,
    // because Store.SetThread/PendingActionUpdater.SetThread capture it once during startup.
    [Fact]
    public void StartupAndLoopStayOnOneDedicatedThreadAcrossPoolCompletedAwaits()
    {
        var ctx = NewContext();
        var cts = new CancellationTokenSource();
        var observed = new List<int>();
        var loopThreadId = 0;
        Exception? failure = null;

        var thread = StartPumpThread("probe-loop", c =>
        {
            try
            {
                loopThreadId = Environment.CurrentManagedThreadId;

                Task startTask = null!;
                c.Start(() => startTask = StartAsync());

                async Task StartAsync()
                {
                    await Task.Yield();
                    observed.Add(Environment.CurrentManagedThreadId);      // where SetThread would run
                    await Task.Run(() => Thread.Sleep(5), cts.Token);      // gRPC completing on the pool
                    observed.Add(Environment.CurrentManagedThreadId);
                    await Task.Delay(10, cts.Token);                       // retry backoff
                    observed.Add(Environment.CurrentManagedThreadId);
                }

                var deadline = Stopwatch.GetTimestamp() + 20 * Stopwatch.Frequency;
                while (!startTask.IsCompleted)
                {
                    c.Pump();
                    if (!startTask.IsCompleted) Thread.Sleep(1);
                    if (Stopwatch.GetTimestamp() > deadline) throw new TimeoutException("startup never completed");
                }
                if (startTask.IsFaulted) throw startTask.Exception!.GetBaseException();

                for (var i = 0; i < 50; i++)
                {
                    c.Pump();
                    observed.Add(Environment.CurrentManagedThreadId);
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        }, ctx);

        Assert.True(thread.Join(JoinTimeoutMs), "pump thread did not finish");
        Assert.Null(failure);
        Assert.Equal(53, observed.Count);
        Assert.All(observed, id => Assert.Equal(loopThreadId, id));
        Assert.NotEqual(Environment.CurrentManagedThreadId, loopThreadId);
    }

    // Cross-thread Send is serviced by the pump and propagates the callback's exception to the caller.
    // This whole branch is untested by the original suite.
    [Fact]
    public void CrossThreadSendIsRunOnThePumpThreadAndPropagatesExceptions()
    {
        var ctx = NewContext();
        var gate = new ManualResetEventSlim(false);
        var pumpThreadId = 0;
        var ranOn = 0;
        Exception? caught = null;

        var pumpThread = StartPumpThread("probe-pump", c =>
        {
            pumpThreadId = Environment.CurrentManagedThreadId;
            c.Pump();          // binds the pump thread id
            gate.Set();
            var deadline = Stopwatch.GetTimestamp() + 20 * Stopwatch.Frequency;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                if (c.Pump() >= 2) return;
                Thread.Sleep(1);
            }
        }, ctx);

        Assert.True(gate.Wait(JoinTimeoutMs));

        ctx.Send(_ => ranOn = Environment.CurrentManagedThreadId, null);
        try { ctx.Send(_ => throw new InvalidOperationException("boom"), null); }
        catch (Exception ex) { caught = ex; }

        Assert.True(pumpThread.Join(JoinTimeoutMs), "pump thread did not finish");
        Assert.Equal(pumpThreadId, ranOn);
        Assert.IsType<InvalidOperationException>(caught);
    }

    // A stopped pump (loop thread exited, crashed, or blocked) must not hang the caller forever.
    [Fact]
    public void CrossThreadSendWithoutAPumpTimesOutRatherThanHanging()
    {
        var ctx = NewContext();
        ctx.SendTimeout = TimeSpan.FromMilliseconds(200);
        ctx.Pump();                                    // bind the pump thread to THIS thread

        Exception? caught = null;
        var sender = new Thread(() =>
        {
            try { ctx.Send(_ => { }, null); }
            catch (Exception ex) { caught = ex; }
        }) { IsBackground = true, Name = "probe-sender" };
        sender.Start();

        Assert.True(sender.Join(JoinTimeoutMs), "Send never returned");
        Assert.IsType<TimeoutException>(caught);
    }

    // The single-pumper rule is now enforced instead of documented: a second pumper is rejected
    // rather than silently taking ownership.
    [Fact]
    public void SecondPumperIsRejected()
    {
        var ctx = NewContext();
        ctx.Pump();
        var first = ctx.PumpThreadId;
        Assert.Equal(Environment.CurrentManagedThreadId, first);

        Exception? caught = null;
        var other = new Thread(() =>
        {
            try { ctx.Pump(); }
            catch (Exception ex) { caught = ex; }
        }) { IsBackground = true, Name = "probe-second-pumper" };
        other.Start();
        Assert.True(other.Join(JoinTimeoutMs));

        Assert.IsType<InvalidOperationException>(caught);
        Assert.Equal(first, ctx.PumpThreadId);         // ownership did not move
    }

    // A faulting async method does NOT reach the pump's catch: the state machine captures the
    // exception into its Task. So the driver loop must inspect IsFaulted itself, otherwise a failed
    // startup passes silently. FailOnErrorLogger proves nothing was logged.
    [Fact]
    public void FaultedAsyncMethodSurfacesOnTheTaskAndNotThroughTheLogger()
    {
        var ctx = NewContext();

        Task task = null!;
        ctx.Start(() => task = Body());
        async Task Body()
        {
            await ctx.Yield();
            throw new InvalidOperationException("startup failed");
        }

        ctx.Pump();

        Assert.True(task.IsFaulted);
        Assert.IsType<InvalidOperationException>(task.Exception!.GetBaseException());
    }

    // Cancelling only takes effect on the next pump, so shutdown must pump at least once more after
    // cancelling or a suspended method never runs its finally.
    [Fact]
    public void CancellationRequiresOneMorePumpToUnwind()
    {
        var ctx = NewContext();
        var cts = new CancellationTokenSource();
        var finallyRan = false;

        Task task = null!;
        ctx.Start(() => task = Body());
        async Task Body()
        {
            try { while (true) await ctx.Yield(cts.Token); }
            finally { finallyRan = true; }
        }

        ctx.Pump();
        cts.Cancel();

        Assert.False(task.IsCompleted);                // cancelling alone does nothing
        Assert.False(finallyRan);

        ctx.Pump();

        Assert.True(task.IsCanceled);
        Assert.True(finallyRan);
    }

    // The whole point of the exercise: a sustained Yield/Pump loop must not allocate per iteration.
    [Fact]
    public void SustainedYieldLoopDoesNotAllocatePerIteration()
    {
        const int Warmup = 200;
        const int Measured = 20_000;

        var ctx = NewContext();
        var cts = new CancellationTokenSource();
        long allocated = 0;
        var iterations = 0;
        Exception? failure = null;

        var thread = StartPumpThread("probe-alloc", c =>
        {
            try
            {
                Task task = null!;
                c.Start(() => task = Body());
                async Task Body()
                {
                    for (var i = 0; i < Warmup + Measured; i++)
                    {
                        await c.Yield(cts.Token);
                        iterations++;
                    }
                }

                for (var i = 0; i < Warmup; i++) c.Pump();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var before = GC.GetAllocatedBytesForCurrentThread();

                for (var i = 0; i < Measured; i++) c.Pump();

                allocated = GC.GetAllocatedBytesForCurrentThread() - before;

                if (!task.IsCompleted) throw new InvalidOperationException("loop did not finish");
                if (task.IsFaulted) throw task.Exception!.GetBaseException();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        }, ctx);

        Assert.True(thread.Join(JoinTimeoutMs), "pump thread did not finish");
        Assert.Null(failure);
        Assert.Equal(Warmup + Measured, iterations);

        var perIteration = (double)allocated / Measured;
        Assert.True(perIteration < 1.0, $"allocated {perIteration:F3} B per pumped iteration (total {allocated} B)");
    }

    // HAZARD, and the most consequential difference from an always-installed context such as
    // Nito's AsyncContextThread: Pump() installs the context only for the duration of the drain and
    // restores the previous one on exit. Async work STARTED from the loop body between pumps
    // therefore captures whatever was ambient (null on a bare thread) and resumes on the POOL.
    [Fact]
    public void WorkStartedBetweenPumpsEscapesToThePool_IsAHazard()
    {
        var ctx = NewContext();
        var loopThreadId = 0;
        var ambientDuringPump = false;
        var ambientInBody = true;
        var resumeThreadId = 0;
        var done = new ManualResetEventSlim(false);

        var thread = StartPumpThread("probe-ambient", c =>
        {
            loopThreadId = Environment.CurrentManagedThreadId;

            c.Post(_ => ambientDuringPump = SynchronizationContext.Current == c, null);
            c.Pump();                                                  // draining: context installed

            // The tick body runs here, outside any pump.
            ambientInBody = SynchronizationContext.Current == c;

            _ = ModWorkAsync();
            async Task ModWorkAsync()
            {
                await Task.Run(() => { });                             // captures Current at the await
                resumeThreadId = Environment.CurrentManagedThreadId;
                done.Set();
            }

            for (var i = 0; i < 200 && !done.IsSet; i++) { c.Pump(); Thread.Sleep(1); }
        }, ctx);

        Assert.True(thread.Join(JoinTimeoutMs), "pump thread did not finish");
        Assert.True(done.Wait(JoinTimeoutMs), "continuation never ran");

        Assert.True(ambientDuringPump);                      // installed only while draining
        Assert.False(ambientInBody);                         // NOT installed in the tick body
        Assert.NotEqual(loopThreadId, resumeThreadId);       // so the continuation escaped to the pool
    }

    // The fix for the hazard above: install the context on the loop thread for its whole lifetime.
    // Pump() then saves and restores it to itself, so it stays ambient in the tick body too.
    [Fact]
    public void InstallingTheContextOnTheLoopThreadKeepsWorkOnIt()
    {
        var ctx = NewContext();
        var loopThreadId = 0;
        var ambientInBody = false;
        var resumeThreadId = 0;
        var done = new ManualResetEventSlim(false);

        var thread = StartPumpThread("probe-ambient-fixed", c =>
        {
            loopThreadId = Environment.CurrentManagedThreadId;
            SynchronizationContext.SetSynchronizationContext(c);       // the one extra line

            c.Pump();
            ambientInBody = SynchronizationContext.Current == c;

            _ = ModWorkAsync();
            async Task ModWorkAsync()
            {
                await Task.Run(() => { });
                resumeThreadId = Environment.CurrentManagedThreadId;
                done.Set();
            }

            for (var i = 0; i < 200 && !done.IsSet; i++) { c.Pump(); Thread.Sleep(1); }
        }, ctx);

        Assert.True(thread.Join(JoinTimeoutMs), "pump thread did not finish");
        Assert.True(done.Wait(JoinTimeoutMs), "continuation never ran");

        Assert.True(ambientInBody);
        Assert.Equal(loopThreadId, resumeThreadId);          // resumed on the loop thread
    }

    // The queue's backing array is reused across pumps, so a burst does not allocate per item
    // forever, but it also never shrinks. Documents the retained-capacity behaviour.
    [Fact]
    public void QueueBackingArrayIsReusedAcrossPumps()
    {
        var ctx = NewContext();
        for (var i = 0; i < 1000; i++) ctx.Post(static _ => { }, null);
        ctx.Pump();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var round = 0; round < 50; round++)
        {
            for (var i = 0; i < 1000; i++) ctx.Post(static _ => { }, null);
            ctx.Pump();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var perItem = (double)allocated / (50 * 1000);
        Assert.True(perItem < 1.0, $"allocated {perItem:F3} B per posted item (total {allocated} B)");
    }
}
