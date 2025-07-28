using System.Threading;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Helpers;

public class PendingActionUpdater<TContext>(TContext context, ILogger logger) : PendingActionScheduler<TContext>(context, logger)
{
    public Thread? Thread
        => _thread;

    public bool Update()
    {
        try
        {
            Monitor.Enter(_lock);
            var count = _typeIndex;

            var hasPendingActions = _group.Update();
            
            for (var i = 0; i < count; i++)
            {
                var group = _groups[i];
                if (group.Update())
                    hasPendingActions = true;
            }

            return hasPendingActions;
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }
    
    public void SetThread(Thread? thread)
    {
        _thread = thread;
    }
}

public class PendingActionUpdater(ILogger logger) : PendingActionScheduler(logger)
{
    public Thread? Thread
        => _thread;

    public bool Update()
    {
        try
        {
            Monitor.Enter(_lock);
            var count = _typeIndex;

            var hasPendingActions = _group.Update();
            
            for (var i = 0; i < count; i++)
            {
                var group = _groups[i];
                if (group.Update())
                    hasPendingActions = true;
            }

            return hasPendingActions;
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }
    
    public void SetThread(Thread? thread)
    {
        _thread = thread;
    }
}