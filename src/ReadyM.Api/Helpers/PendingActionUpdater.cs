using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Helpers;

internal class PendingActionUpdater<TContext>(TContext context, ILogger logger) : PendingActionScheduler<TContext>(context, logger)
{
    public Thread? Thread
        => thread;

    private bool _insideUpdate;

    public override bool Update()
    {
        if (_insideUpdate)
            return false;

        try
        {
            _insideUpdate = true;
            Monitor.Enter(_lock);

            (_oldQueue, _queue) = (_queue, _oldQueue);
            var queueCount = _queueIndex;
            _queue.Clear();
            _queueIndex = 0;

            for (var i = 0; i < queueCount; i++)
            {
                var group = _oldQueue[i];
                _oldQueue[i] = null;
                group!.Update();
            }

            return queueCount > 0;
        }
        finally
        {
            Monitor.Exit(_lock);
            _insideUpdate = false;
        }
    }

    public void SetThread(Thread? newThread, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
    {
        logger.LogDebug("Setting thread to {ThreadName} (ID: {ThreadId}) at {Class}:{Line} ", newThread?.Name ?? "null", newThread?.ManagedThreadId ?? -1, callerFile, callerLine);
        thread = newThread;
    }
}