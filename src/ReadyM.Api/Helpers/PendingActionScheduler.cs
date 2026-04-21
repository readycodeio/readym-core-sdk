using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Helpers;

public abstract class PendingActionScheduler<TContext> : PendingActionSchedulerBase
{
    protected class PooledCompletionSource<T> : IValueTaskSource<T>
    {
        private ManualResetValueTaskSourceCore<T> _core;

        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token)
            => _core.GetStatus(token);

        void IValueTaskSource<T>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);
        
        T IValueTaskSource<T>.GetResult(short token)
            => _core.GetResult(token);
        
        public void SetResult(T result)
            => _core.SetResult(result);
        
        public void SetException(Exception ex)
            => _core.SetException(ex);

        public ValueTask<T> Task
            => new(this, _core.Version);

        public void Reset()
            => _core.Reset();
    }
    
    protected abstract class PendingGroupBase
    {
        // NOTE: By convention this function is called from inside the `_lock` monitor.
        public abstract void Update();

        // ReSharper disable once StaticMemberInGenericType
        protected static int TypeIndexCounter;
    }
    
    protected class PendingActionGroup(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext>, PooledCompletionSource<bool>?)> _oldItems = new(MaxPendingGroupItemCount);
        private List<(Action<TContext>, PooledCompletionSource<bool>?)> _items = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<bool> AddAsync(Action<TContext> action)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _items.Add((action, tcs));
            return tcs;
        }

        public void Add(Action<TContext> action)
            => _items.Add((action, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (action, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    action.Invoke(owner._context);
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }
    
    protected class PendingActionGroup<T>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int TypeIndex = TypeIndexCounter++;
        
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T>, T, PooledCompletionSource<bool>?)> _items = new(MaxPendingGroupItemCount);
        private List<(Action<TContext, T>, T, PooledCompletionSource<bool>?)> _oldItems = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T> action, T arg)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }

            _items.Add((action, arg, tcs));
            return tcs;
        }

        public void Add(Action<TContext, T> action, T arg)
            => _items.Add((action, arg, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (action, arg, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    action.Invoke(owner._context, arg);
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }

    protected class PendingActionGroup<T0, T1>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int TypeIndex = TypeIndexCounter++;

        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T0, T1>, T0, T1, PooledCompletionSource<bool>?)> _items = new(MaxPendingGroupItemCount);
        private List<(Action<TContext, T0, T1>, T0, T1, PooledCompletionSource<bool>?)> _oldItems = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }

            _items.Add((action, arg0, arg1, tcs));
            return tcs;
        }

        public void Add(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
            => _items.Add((action, arg0, arg1, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (action, arg0, arg1, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    action.Invoke(owner._context, arg0, arg1);
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }

    protected class PendingActionGroup<T0, T1, T2>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int TypeIndex = TypeIndexCounter++;

        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T0, T1, T2>, T0, T1, T2, PooledCompletionSource<bool>?)> _items = new(MaxPendingGroupItemCount);
        private List<(Action<TContext, T0, T1, T2>, T0, T1, T2, PooledCompletionSource<bool>?)> _oldItems = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }

            _items.Add((action, arg0, arg1, arg2, tcs));
            return tcs;
        }

        public void Add(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
            => _items.Add((action, arg0, arg1, arg2, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (action, arg0, arg1, arg2, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    action.Invoke(owner._context, arg0, arg1, arg2);
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }

    protected class PendingActionGroup<T0, T1, T2, T3>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int TypeIndex = TypeIndexCounter++;

        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T0, T1, T2, T3>, T0, T1, T2, T3, PooledCompletionSource<bool>?)> _items = new(MaxPendingGroupItemCount);
        private List<(Action<TContext, T0, T1, T2, T3>, T0, T1, T2, T3, PooledCompletionSource<bool>?)> _oldItems = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }

            _items.Add((action, arg0, arg1, arg2, arg3, tcs));
            return tcs;
        }

        public void Add(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
            => _items.Add((action, arg0, arg1, arg2, arg3, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (action, arg0, arg1, arg2, arg3, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    action.Invoke(owner._context, arg0, arg1, arg2, arg3);
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }

    protected class PendingActionGroup<T0, T1, T2, T3, T4>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int TypeIndex = TypeIndexCounter++;

        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T0, T1, T2, T3, T4>, T0, T1, T2, T3, T4, PooledCompletionSource<bool>?)> _items = new(MaxPendingGroupItemCount);
        private List<(Action<TContext, T0, T1, T2, T3, T4>, T0, T1, T2, T3, T4, PooledCompletionSource<bool>?)> _oldItems = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }

            _items.Add((action, arg0, arg1, arg2, arg3, arg4, tcs));
            return tcs;
        }

        public void Add(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => _items.Add((action, arg0, arg1, arg2, arg3, arg4, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (action, arg0, arg1, arg2, arg3, arg4, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    action.Invoke(owner._context, arg0, arg1, arg2, arg3, arg4);
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }

    protected class PendingFuncGroup<TResult>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int TypeIndex = TypeIndexCounter++;

        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<TContext, TResult>, PooledCompletionSource<TResult>?)> _items = new(MaxPendingGroupItemCount);
        private List<(Func<TContext, TResult>, PooledCompletionSource<TResult>?)> _oldItems = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<TResult> AddAsync(Func<TContext, TResult> action)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }

            _items.Add((action, tcs));
            return tcs;
        }

        public void Add(Func<TContext, TResult> func)
            => _items.Add((func, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (func, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    var result = func.Invoke(owner._context);
                    tcs?.SetResult(result);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }

    protected class PendingFuncGroup<T, TResult>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int TypeIndex = TypeIndexCounter++;

        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<TContext, T, TResult>, T, PooledCompletionSource<TResult>?)> _items = new(MaxPendingGroupItemCount);
        private List<(Func<TContext, T, TResult>, T, PooledCompletionSource<TResult>?)> _oldItems = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<TResult> AddAsync(Func<TContext, T, TResult> func, T arg)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }

            _items.Add((func, arg, tcs));
            return tcs;
        }

        public void Add(Func<TContext, T, TResult> func, T arg)
            => _items.Add((func, arg, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (func, arg, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    var result = func.Invoke(owner._context, arg);
                    tcs?.SetResult(result);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }

    protected class PendingFuncGroup<T0, T1, TResult>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int TypeIndex = TypeIndexCounter++;

        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<TContext, T0, T1, TResult>, T0, T1, PooledCompletionSource<TResult>?)> _items = new(MaxPendingGroupItemCount);
        private List<(Func<TContext, T0, T1, TResult>, T0, T1, PooledCompletionSource<TResult>?)> _oldItems = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<TResult> AddAsync(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }

            _items.Add((func, arg0, arg1, tcs));
            return tcs;
        }

        public void Add(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
            => _items.Add((func, arg0, arg1, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (func, arg0, arg1, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    var result = func.Invoke(owner._context, arg0, arg1);
                    tcs?.SetResult(result);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }

    protected class PendingFuncGroup<T0, T1, T2, TResult>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int TypeIndex = TypeIndexCounter++;

        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<TContext, T0, T1, T2, TResult>, T0, T1, T2, PooledCompletionSource<TResult>?)> _items = new(MaxPendingGroupItemCount);
        private List<(Func<TContext, T0, T1, T2, TResult>, T0, T1, T2, PooledCompletionSource<TResult>?)> _oldItems = new(MaxPendingGroupItemCount);
        private int _updateIndex;

        public PooledCompletionSource<TResult> AddAsync(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }

            _items.Add((func, arg0, arg1, arg2, tcs));
            return tcs;
        }

        public void Add(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
            => _items.Add((func, arg0, arg1, arg2, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override void Update()
        {
            if (_updateIndex >= _oldItems.Count)
                Reset();
            var (func, arg0, arg1, arg2, tcs) = _oldItems[_updateIndex++];

            try
            {
                Monitor.Exit(owner._lock);

                try
                {
                    var result = func.Invoke(owner._context, arg0, arg1, arg2);
                    tcs?.SetResult(result);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            finally
            {
                Monitor.Enter(owner._lock);
            }
        }

        private void Reset()
        {
            _oldItems.Clear();
            (_oldItems, _items) = (_items, _oldItems);
            _updateIndex = 0;
        }
    }
    
    private readonly ILogger _logger;

    protected readonly object _lock = new();
    protected readonly PendingActionGroup _group;
    protected readonly PendingGroupBase[] _groups = new PendingGroupBase[256];

    protected List<PendingGroupBase?> _queue = new(MaxPendingItemCount);
    protected List<PendingGroupBase?> _oldQueue = new(MaxPendingItemCount);
    protected int _queueIndex;
    
    protected int _delayedCount;

    protected bool IsDelayed
        => _delayedCount > 0;

    protected TContext _context;
    
    protected PendingActionScheduler(TContext context, ILogger logger)
    {
        _logger = logger;
        _group = new(this);
        _context = context;
    }

    public void BeginDelay()
    {
        _delayedCount++;
    }
    
    public void EndDelay()
    {
        if (_delayedCount <= 0)
            throw new InvalidOperationException("No delay in progress");
        _delayedCount--;
    }

    public void SetContext(TContext context)
    {
        _context = context;
    }
    
    public async ValueTask RunAsync(Action<TContext> action)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            // NOTE: We only guarantee that calls from the same thread will be executed in order.
            if (_queueIndex > 0)
                Update();
            action(_context);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        
        lock (_lock)
        {
            tcs = _group.AddAsync(action);

            _queueIndex++;
            _queue.Add(_group);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            _group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T>(Action<TContext, T> action, T arg)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            // NOTE: We only guarantee that calls from the same thread will be executed in order.
            if (_queueIndex > 0)
                Update();
            action(_context, arg);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T>? group;

        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T>.TypeIndex;
            group = (PendingActionGroup<T>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg);

            _queueIndex++;
            _queue.Add(group);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1>(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            // NOTE: We only guarantee that calls from the same thread will be executed in order.
            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1>.TypeIndex;
            group = (PendingActionGroup<T0, T1>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg0, arg1);
            
            _queueIndex++;
            _queue.Add(group);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1, T2>(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1, arg2);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2>.TypeIndex;
            group = (PendingActionGroup<T0, T1, T2>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1, T2>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg0, arg1, arg2);

            _queueIndex++;
            _queue.Add(group);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1, T2, T3>(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1, arg2, arg3);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2, T3>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3>.TypeIndex;
            group = (PendingActionGroup<T0, T1, T2, T3>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1, T2, T3>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg0, arg1, arg2, arg3);
            
            _queueIndex++;
            _queue.Add(group);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1, T2, T3, T4>(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1, arg2, arg3, arg4);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2, T3, T4>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3, T4>.TypeIndex;
            group = (PendingActionGroup<T0, T1, T2, T3, T4>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1, T2, T3, T4>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg0, arg1, arg2, arg3, arg4);
            
            _queueIndex++;
            _queue.Add(group);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }

    public async ValueTask<TResult> RunFuncAsync<TResult>(Func<TContext, TResult> func)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            if (_queueIndex > 0)
                Update();
            return func(_context);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<TResult>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<TResult>.TypeIndex;
            group = (PendingFuncGroup<TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<TResult>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(func);

            _queueIndex++;
            _queue.Add(group);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public async ValueTask<TResult> RunFuncAsync<T, TResult>(Func<TContext, T, TResult> func, T arg)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            if (_queueIndex > 0)
                Update();
            return func(_context, arg);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T, TResult>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T, TResult>.TypeIndex;
            group = (PendingFuncGroup<T, TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<T, TResult>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(func, arg);
            
            _queueIndex++;
            _queue.Add(group);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public async ValueTask<TResult> RunFuncAsync<T0, T1, TResult>(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            if (_queueIndex > 0)
                Update();
            return func(_context, arg0, arg1);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, TResult>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, TResult>.TypeIndex;
            group = (PendingFuncGroup<T0, T1, TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<T0, T1, TResult>(this);
                _groups[typeIndex] = group;
            }

            tcs = group.AddAsync(func, arg0, arg1);
            
            _queueIndex++;
            _queue.Add(group);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public async ValueTask<TResult> RunFuncAsync<T0, T1, T2, TResult>(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            if (_queueIndex > 0)
                Update();
            return func(_context, arg0, arg1, arg2);
        }
        
        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, T2, TResult>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex;
            group = (PendingFuncGroup<T0, T1, T2, TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<T0, T1, T2, TResult>(this);
                _groups[typeIndex] = group;
            }

            tcs = group.AddAsync(func, arg0, arg1, arg2);
            
            _queueIndex++;
            _queue.Add(group);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public void Schedule(Action<TContext> action)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            action(_context);
            return;
        }
        
        lock (_lock)
        {
            _group.Add(action);

            _queueIndex++;
            _queue.Add(_group);
        }
    }
    
    public void Schedule<T>(Action<TContext, T> action, T arg)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            action(_context, arg);
            return;
        }
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T>.TypeIndex;
            var group = (PendingActionGroup<T>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T>(this);
                _groups[typeIndex] = group;
            }

            group.Add(action, arg);

            _queueIndex++;
            _queue.Add(group);
        }
    }
    
    public void Schedule<T0, T1>(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1);
            return;
        }
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1>.TypeIndex;
            var group = (PendingActionGroup<T0, T1>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1>(this);
                _groups[typeIndex] = group;
            }

            group.Add(action, arg0, arg1);
            
            _queueIndex++;
            _queue.Add(group);
        }
    }
    
    public void Schedule<T0, T1, T2>(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1, arg2);
            return;
        }
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2>.TypeIndex;
            var group = (PendingActionGroup<T0, T1, T2>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1, T2>(this);
                _groups[typeIndex] = group;
            }

            group.Add(action, arg0, arg1, arg2);
            
            _queueIndex++;
            _queue.Add(group);
        }
    }
    
    public void Schedule<T0, T1, T2, T3>(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1, arg2, arg3);
            return;
        }
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3>.TypeIndex;
            var group = (PendingActionGroup<T0, T1, T2, T3>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1, T2, T3>(this);
                _groups[typeIndex] = group;
            }

            group.Add(action, arg0, arg1, arg2, arg3);
            
            _queueIndex++;
            _queue.Add(group);
        }
    }
    
    public void Schedule<T0, T1, T2, T3, T4>(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1, arg2, arg3, arg4);
            return;
        }
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3, T4>.TypeIndex;
            var group = (PendingActionGroup<T0, T1, T2, T3, T4>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1, T2, T3, T4>(this);
                _groups[typeIndex] = group;
            }

            group.Add(action, arg0, arg1, arg2, arg3, arg4);
            
            _queueIndex++;
            _queue.Add(group);
        }
    }

    public void ScheduleFunc<TResult>(Func<TContext, TResult> func)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            func(_context);
            return;
        }
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<TResult>.TypeIndex;
            var group = (PendingFuncGroup<TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<TResult>(this);
                _groups[typeIndex] = group;
            }
            
            group.Add(func);
            
            _queueIndex++;
            _queue.Add(group);
        }
    }
    
    public void ScheduleFunc<T, TResult>(Func<TContext, T, TResult> func, T arg)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            func(_context, arg);
            return;
        }
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T, TResult>.TypeIndex;
            var group = (PendingFuncGroup<T, TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<T, TResult>(this);
                _groups[typeIndex] = group;
            }
            
            group.Add(func, arg);
            
            _queueIndex++;
            _queue.Add(group);
        }
    }
    
    public void ScheduleFunc<T0, T1, TResult>(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            func(_context, arg0, arg1);
            return;
        }
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, TResult>.TypeIndex;
            var group = (PendingFuncGroup<T0, T1, TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<T0, T1, TResult>(this);
                _groups[typeIndex] = group;
            }
            
            group.Add(func, arg0, arg1);
            
            _queueIndex++;
            _queue.Add(group);
        }
    }
    
    public void ScheduleFunc<T0, T1, T2, TResult>(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
    {
        if ((thread == null || thread == Thread.CurrentThread) && !IsDelayed)
        {
            if (_queueIndex > 0)
                Update();
            func(_context, arg0, arg1, arg2);
            return;
        }
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex;
            var group = (PendingFuncGroup<T0, T1, T2, TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<T0, T1, T2, TResult>(this);
                _groups[typeIndex] = group;
            }
            
            group.Add(func, arg0, arg1, arg2);
            
            _queueIndex++;
            _queue.Add(group);
        }
    }
    
    public void RunSynchronously(Action<TContext> action)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            if (_queueIndex > 0)
                Update();
            action(_context);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        
        lock (_lock)
        {
            tcs = _group.AddAsync(action);
            
            _queueIndex++;
            _queue.Add(_group);
        }

        tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            _group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T>(Action<TContext, T> action, T arg)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();
            
            if (_queueIndex > 0)
                Update();
            action(_context, arg);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T>.TypeIndex;
            group = (PendingActionGroup<T>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg);
            
            _queueIndex++;
            _queue.Add(group);
        }

        tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1>(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();

            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1>.TypeIndex;
            group = (PendingActionGroup<T0, T1>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg0, arg1);
            
            _queueIndex++;
            _queue.Add(group);
        }

        tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1, T2>(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();

            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1, arg2);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2>.TypeIndex;
            group = (PendingActionGroup<T0, T1, T2>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1, T2>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg0, arg1, arg2);
            
            _queueIndex++;
            _queue.Add(group);
        }

        tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1, T2, T3>(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();

            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1, arg2, arg3);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2, T3>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3>.TypeIndex;
            group = (PendingActionGroup<T0, T1, T2, T3>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1, T2, T3>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg0, arg1, arg2, arg3);
            
            _queueIndex++;
            _queue.Add(group);
        }

        tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1, T2, T3, T4>(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();

            if (_queueIndex > 0)
                Update();
            action(_context, arg0, arg1, arg2, arg3, arg4);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2, T3, T4>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3, T4>.TypeIndex;
            group = (PendingActionGroup<T0, T1, T2, T3, T4>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingActionGroup<T0, T1, T2, T3, T4>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(action, arg0, arg1, arg2, arg3, arg4);
            
            _queueIndex++;
            _queue.Add(group);
        }

        tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }

    public TResult RunSynchronously<TResult>(Func<TContext, TResult> func)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();

            if (_queueIndex > 0)
                Update();
            return func(_context);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<TResult>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<TResult>.TypeIndex;
            group = (PendingFuncGroup<TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<TResult>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(func);
            
            _queueIndex++;
            _queue.Add(group);
        }

        var result = tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public TResult RunSynchronously<T, TResult>(Func<TContext, T, TResult> func, T arg)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();

            if (_queueIndex > 0)
                Update();
            return func(_context, arg);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T, TResult>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T, TResult>.TypeIndex;
            group = (PendingFuncGroup<T, TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<T, TResult>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(func, arg);
            
            _queueIndex++;
            _queue.Add(group);
        }

        var result = tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public TResult RunSynchronously<T0, T1, TResult>(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
    {
        if (IsDelayed)
            throw new InvalidOperationException();

        if (thread == null || thread == Thread.CurrentThread)
        {
            if (_queueIndex > 0)
                Update();
            return func(_context, arg0, arg1);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, TResult>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, TResult>.TypeIndex;
            group = (PendingFuncGroup<T0, T1, TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<T0, T1, TResult>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(func, arg0, arg1);
            
            _queueIndex++;
            _queue.Add(group);
        }

        var result = tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public TResult RunSynchronously<T0, T1, T2, TResult>(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
    {
        if (thread == null || thread == Thread.CurrentThread)
        {
            if (IsDelayed)
                throw new InvalidOperationException();

            if (_queueIndex > 0)
                Update();
            return func(_context, arg0, arg1, arg2);
        }
        
        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, T2, TResult>? group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex;
            group = (PendingFuncGroup<T0, T1, T2, TResult>?)_groups[typeIndex];
            if (group == null)
            {
                group = new PendingFuncGroup<T0, T1, T2, TResult>(this);
                _groups[typeIndex] = group;
            }
            
            tcs = group.AddAsync(func, arg0, arg1, arg2);
            
            _queueIndex++;
            _queue.Add(group);
        }

        var result = tcs.Task.AsTask().GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
}