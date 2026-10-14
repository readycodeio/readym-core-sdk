using Microsoft.Extensions.Logging;
using ReadyM.Api.Helpers;

namespace ReadyM.Api.Tests;

/// <summary>
/// While the scheduler is held, a call made on its own thread runs at once and leaves the queued actions queued; they
/// run on the first update after the hold ends. The server holds it while a mod seeder runs.
/// </summary>
public class PendingActionUpdaterHoldTests
{
    private static PendingActionUpdater<List<string>> NewUpdater(List<string> ran)
    {
        var updater = new PendingActionUpdater<List<string>>(
            ran, LoggerFactory.Create(builder => builder.AddProvider(new FailOnErrorLoggerProvider())).CreateLogger("Tests"));
        updater.SetThread(Thread.CurrentThread);
        return updater;
    }

    // Queued the way another thread's action is: on this thread a delayed schedule waits in the queue.
    private static void Queue(PendingActionUpdater<List<string>> updater, string name)
    {
        updater.BeginDelay();
        updater.Schedule(ran => ran.Add(name));
        updater.EndDelay();
    }

    [Fact]
    public void AHostCallWhileHeldRunsNoQueuedAction()
    {
        var ran = new List<string>();
        var updater = NewUpdater(ran);
        Queue(updater, "queued");

        updater.BeginHold();
        updater.RunSynchronously(r => r.Add("inline"));

        Assert.Equal(new[] { "inline" }, ran);
    }

    [Fact]
    public void TheQueuedActionRunsAfterTheHoldEnds()
    {
        var ran = new List<string>();
        var updater = NewUpdater(ran);
        Queue(updater, "queued");

        updater.BeginHold();
        updater.RunSynchronously(r => r.Add("inline"));
        updater.EndHold();
        updater.Update();

        Assert.Equal(new[] { "inline", "queued" }, ran);
    }
}
