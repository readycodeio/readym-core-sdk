using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Helpers;

public abstract class PendingActionScheduler : PendingActionSchedulerBase
{
    protected class PooledCompletionSource<T> : IValueTaskSource<T>
    {
        private ManualResetValueTaskSourceCore<T> _core;

        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token)
            => _core.GetStatus(token);

        void IValueTaskSource<T>.OnCompleted(Action<object?> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
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
        public abstract bool Update();
    }
    
    protected class PendingActionGroup(PendingActionScheduler owner) : PendingGroupBase
    {
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action action)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, tcs));
            return tcs;
        }

        public void Add(Action action)
            => _newItems.Add((action, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, tcs) in _items)
            {
                try
                {
                    action.Invoke();
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            Monitor.Enter(owner._lock);
            return true;
        }
    }

    protected class PendingActionGroup<T>(PendingActionScheduler owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<T>, T, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action<T>, T, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action<T> action, T arg)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, arg, tcs));

            return tcs;
        }

        public void Add(Action<T> action, T arg)
            => _newItems.Add((action, arg, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, arg, tcs) in _items)
            {
                try
                {
                    action.Invoke(arg);
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingActionGroup<T0, T1>(PendingActionScheduler owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<T0, T1>, T0, T1, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action<T0, T1>, T0, T1, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action<T0, T1> action, T0 arg0, T1 arg1)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, arg0, arg1, tcs));

            return tcs;
        }
        
        public void Add(Action<T0, T1> action, T0 arg0, T1 arg1)
            => _newItems.Add((action, arg0, arg1, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, arg0, arg1, tcs) in _items)
            {
                try
                {
                    action.Invoke(arg0, arg1);
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingActionGroup<T0, T1, T2>(PendingActionScheduler owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<T0, T1, T2>, T0, T1, T2, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action<T0, T1, T2>, T0, T1, T2, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, arg0, arg1, arg2, tcs));

            return tcs;
        }
        
        public void Add(Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
            => _newItems.Add((action, arg0, arg1, arg2, null));
        
        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, arg0, arg1, arg2, tcs) in _items)
            {
                try
                {
                    action.Invoke(arg0, arg1, arg2);
                    tcs?.SetResult(true);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingFuncGroup<TResult>(PendingActionScheduler owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;

        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<TResult>, PooledCompletionSource<TResult>?)> _items = new();
        private List<(Func<TResult>, PooledCompletionSource<TResult>?)> _newItems = new();

        public PooledCompletionSource<TResult> AddAsync(Func<TResult> action)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }
            
            _newItems.Add((action, tcs));

            return tcs;
        }

        public void Add(Func<TResult> func)
            => _newItems.Add((func, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (func, tcs) in _items)
            {
                try
                {
                    var result = func.Invoke();
                    tcs?.SetResult(result);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingFuncGroup<T, TResult>(PendingActionScheduler owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;

        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<T, TResult>, T, PooledCompletionSource<TResult>?)> _items = new();
        private List<(Func<T, TResult>, T, PooledCompletionSource<TResult>?)> _newItems = new();

        public PooledCompletionSource<TResult> AddAsync(Func<T, TResult> func, T arg)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }
            
            _newItems.Add((func, arg, tcs));

            return tcs;
        }
        
        public void Add(Func<T, TResult> func, T arg)
            => _newItems.Add((func, arg, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (func, arg, tcs) in _items)
            {
                try
                {
                    var result = func.Invoke(arg);
                    tcs?.SetResult(result);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingFuncGroup<T0, T1, TResult>(PendingActionScheduler owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<T0, T1, TResult>, T0, T1, PooledCompletionSource<TResult>?)> _items = new();
        private List<(Func<T0, T1, TResult>, T0, T1, PooledCompletionSource<TResult>?)> _newItems = new();

        public PooledCompletionSource<TResult> AddAsync(Func<T0, T1, TResult> func, T0 arg0, T1 arg1)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }
            
            _newItems.Add((func, arg0, arg1, tcs));

            return tcs;
        }

        public void Add(Func<T0, T1, TResult> func, T0 arg0, T1 arg1)
            => _newItems.Add((func, arg0, arg1, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (func, arg0, arg1, tcs) in _items)
            {
                try
                {
                    var result = func.Invoke(arg0, arg1);
                    tcs?.SetResult(result);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingFuncGroup<T0, T1, T2, TResult>(PendingActionScheduler owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<T0, T1, T2, TResult>, T0, T1, T2, PooledCompletionSource<TResult>?)> _items = new();
        private List<(Func<T0, T1, T2, TResult>, T0, T1, T2, PooledCompletionSource<TResult>?)> _newItems = new();

        public PooledCompletionSource<TResult> AddAsync(Func<T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }
            
            _newItems.Add((func, arg0, arg1, arg2, tcs));

            return tcs;
        }

        public void Add(Func<T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
            => _newItems.Add((func, arg0, arg1, arg2, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (func, arg0, arg1, arg2, tcs) in _items)
            {
                try
                {
                    var result = func.Invoke(arg0, arg1, arg2);
                    tcs?.SetResult(result);
                }
                catch (Exception ex)
                {
                    owner._logger.LogError(ex, "Error executing pending action");
                    tcs?.SetException(ex);
                }
            }
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    private readonly ILogger _logger;

    protected readonly object _lock = new();
    protected int _typeIndex;
    protected readonly PendingActionGroup _group;
    protected readonly PendingGroupBase[] _groups = new PendingGroupBase[256];

    protected PendingActionScheduler(ILogger logger)
    {
        _logger = logger;
        _group = new(this);
    }
    
    public async ValueTask RunAsync(Action action)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action();
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        
        lock (_lock)
        {
            tcs = _group.AddAsync(action);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            _group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T>(Action<T> action, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(arg);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1>(Action<T0, T1> action, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(arg0, arg1);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1, T2>(Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(arg0, arg1, arg2);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1, arg2);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask<TResult> RunAsync<TResult>(Func<TResult> func)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func();
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public async ValueTask<TResult> RunAsync<T, TResult>(Func<T, TResult> func, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(arg);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public async ValueTask<TResult> RunAsync<T0, T1, TResult>(Func<T0, T1, TResult> func, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(arg0, arg1);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg0, arg1);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public async ValueTask<TResult> RunAsync<T0, T1, T2, TResult>(Func<T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(arg0, arg1, arg2);
        }
        
        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, T2, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, T2, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, T2, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg0, arg1, arg2);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public void Schedule(Action action)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action();
            return;
        }
        
        lock (_lock)
        {
            _group.Add(action);
        }
    }
    
    public void Schedule<T>(Action<T> action, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(arg);
            return;
        }
        
        lock (_lock)
        {
            PendingActionGroup<T> group;
            var typeIndex = PendingActionGroup<T>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T>)_groups[typeIndex];
            }

            group.Add(action, arg);
        }
    }
    
    public void Schedule<T0, T1>(Action<T0, T1> action, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(arg0, arg1);
            return;
        }
        
        lock (_lock)
        {
            PendingActionGroup<T0, T1> group;
            var typeIndex = PendingActionGroup<T0, T1>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1>)_groups[typeIndex];
            }

            group.Add(action, arg0, arg1);
        }
    }
    
    public void Schedule<T0, T1, T2>(Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(arg0, arg1, arg2);
            return;
        }
        
        lock (_lock)
        {
            PendingActionGroup<T0, T1, T2> group;
            var typeIndex = PendingActionGroup<T0, T1, T2>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2>)_groups[typeIndex];
            }

            group.Add(action, arg0, arg1, arg2);
        }
    }
    
    public void Schedule<TResult>(Func<TResult> func)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            func();
            return;
        }
        
        lock (_lock)
        {
            PendingFuncGroup<TResult> group;
            var typeIndex = PendingFuncGroup<TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<TResult>)_groups[typeIndex];
            }

            group.Add(func);
        }
    }
    
    public void Schedule<T, TResult>(Func<T, TResult> func, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            func(arg);
            return;
        }
        
        lock (_lock)
        {
            PendingFuncGroup<T, TResult> group;
            var typeIndex = PendingFuncGroup<T, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T, TResult>)_groups[typeIndex];
            }

            group.Add(func, arg);
        }
    }
    
    public void Schedule<T0, T1, TResult>(Func<T0, T1, TResult> func, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            func(arg0, arg1);
            return;
        }
        
        lock (_lock)
        {
            PendingFuncGroup<T0, T1, TResult> group;
            var typeIndex = PendingFuncGroup<T0, T1, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, TResult>)_groups[typeIndex];
            }

            group.Add(func, arg0, arg1);
        }
    }
    
    public void Schedule<T0, T1, T2, TResult>(Func<T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            func(arg0, arg1, arg2);
            return;
        }
        
        lock (_lock)
        {
            PendingFuncGroup<T0, T1, T2, TResult> group;
            var typeIndex = PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, T2, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, T2, TResult>)_groups[typeIndex];
            }

            group.Add(func, arg0, arg1, arg2);
        }
    }
    
    public void RunSynchronously(Action action)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action();
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        
        lock (_lock)
        {
            tcs = _group.AddAsync(action);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            _group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T>(Action<T> action, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(arg);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1>(Action<T0, T1> action, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(arg0, arg1);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1, T2>(Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(arg0, arg1, arg2);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1, arg2);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public TResult RunSynchronously<TResult>(Func<TResult> func)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func();
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func);
        }

        var result = tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public TResult RunSynchronously<T, TResult>(Func<T, TResult> func, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(arg);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg);
        }

        var result = tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public TResult RunSynchronously<T0, T1, TResult>(Func<T0, T1, TResult> func, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(arg0, arg1);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg0, arg1);
        }

        var result = tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public TResult RunSynchronously<T0, T1, T2, TResult>(Func<T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(arg0, arg1, arg2);
        }
        
        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, T2, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, T2, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, T2, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg0, arg1, arg2);
        }

        var result = tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
}

public abstract class PendingActionScheduler<TContext> : PendingActionSchedulerBase
{
    protected class PooledCompletionSource<T> : IValueTaskSource<T>
    {
        private ManualResetValueTaskSourceCore<T> _core;

        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token)
            => _core.GetStatus(token);

        void IValueTaskSource<T>.OnCompleted(Action<object?> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
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
        public abstract bool Update();
    }
    
    protected class PendingActionGroup(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext>, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action<TContext>, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action<TContext> action)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, tcs));
            return tcs;
        }

        public void Add(Action<TContext> action)
            => _newItems.Add((action, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }

    protected class PendingActionGroup<T>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T>, T, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action<TContext, T>, T, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T> action, T arg)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, arg, tcs));

            return tcs;
        }

        public void Add(Action<TContext, T> action, T arg)
            => _newItems.Add((action, arg, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }

        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, arg, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingActionGroup<T0, T1>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T0, T1>, T0, T1, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action<TContext, T0, T1>, T0, T1, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, arg0, arg1, tcs));

            return tcs;
        }
        
        public void Add(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
            => _newItems.Add((action, arg0, arg1, null));

        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, arg0, arg1, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingActionGroup<T0, T1, T2>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T0, T1, T2>, T0, T1, T2, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action<TContext, T0, T1, T2>, T0, T1, T2, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, arg0, arg1, arg2, tcs));

            return tcs;
        }
        
        public void Add(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
            => _newItems.Add((action, arg0, arg1, arg2, null));
        
        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, arg0, arg1, arg2, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingActionGroup<T0, T1, T2, T3>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T0, T1, T2, T3>, T0, T1, T2, T3, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action<TContext, T0, T1, T2, T3>, T0, T1, T2, T3, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, arg0, arg1, arg2, arg3, tcs));

            return tcs;
        }
        
        public void Add(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
            => _newItems.Add((action, arg0, arg1, arg2, arg3, null));
        
        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, arg0, arg1, arg2, arg3, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }

    protected class PendingActionGroup<T0, T1, T2, T3, T4>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<bool>> _sources = new();
        private List<(Action<TContext, T0, T1, T2, T3, T4>, T0, T1, T2, T3, T4, PooledCompletionSource<bool>?)> _items = new();
        private List<(Action<TContext, T0, T1, T2, T3, T4>, T0, T1, T2, T3, T4, PooledCompletionSource<bool>?)> _newItems = new();

        public PooledCompletionSource<bool> AddAsync(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<bool>();
            }
            
            _newItems.Add((action, arg0, arg1, arg2, arg3, arg4, tcs));

            return tcs;
        }
        
        public void Add(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => _newItems.Add((action, arg0, arg1, arg2, arg3, arg4, null));
        
        public void Release(PooledCompletionSource<bool> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (action, arg0, arg1, arg2, arg3, arg4, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingFuncGroup<TResult>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;

        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<TContext, TResult>, PooledCompletionSource<TResult>?)> _items = new();
        private List<(Func<TContext, TResult>, PooledCompletionSource<TResult>?)> _newItems = new();

        public PooledCompletionSource<TResult> AddAsync(Func<TContext, TResult> action)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }
            
            _newItems.Add((action, tcs));

            return tcs;
        }

        public void Add(Func<TContext, TResult> func)
            => _newItems.Add((func, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (func, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingFuncGroup<T, TResult>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;

        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<TContext, T, TResult>, T, PooledCompletionSource<TResult>?)> _items = new();
        private List<(Func<TContext, T, TResult>, T, PooledCompletionSource<TResult>?)> _newItems = new();

        public PooledCompletionSource<TResult> AddAsync(Func<TContext, T, TResult> func, T arg)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }
            
            _newItems.Add((func, arg, tcs));

            return tcs;
        }
        
        public void Add(Func<TContext, T, TResult> func, T arg)
            => _newItems.Add((func, arg, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (func, arg, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingFuncGroup<T0, T1, TResult>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<TContext, T0, T1, TResult>, T0, T1, PooledCompletionSource<TResult>?)> _items = new();
        private List<(Func<TContext, T0, T1, TResult>, T0, T1, PooledCompletionSource<TResult>?)> _newItems = new();

        public PooledCompletionSource<TResult> AddAsync(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }
            
            _newItems.Add((func, arg0, arg1, tcs));

            return tcs;
        }

        public void Add(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
            => _newItems.Add((func, arg0, arg1, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (func, arg0, arg1, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    protected class PendingFuncGroup<T0, T1, T2, TResult>(PendingActionScheduler<TContext> owner) : PendingGroupBase
    {
        // ReSharper disable once StaticMemberInGenericType
        public static int TypeIndex = -1;
        
        private readonly Stack<PooledCompletionSource<TResult>> _sources = new();
        private List<(Func<TContext, T0, T1, T2, TResult>, T0, T1, T2, PooledCompletionSource<TResult>?)> _items = new();
        private List<(Func<TContext, T0, T1, T2, TResult>, T0, T1, T2, PooledCompletionSource<TResult>?)> _newItems = new();

        public PooledCompletionSource<TResult> AddAsync(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
        {
            if (!_sources.TryPop(out var tcs))
            {
                tcs = new PooledCompletionSource<TResult>();
            }
            
            _newItems.Add((func, arg0, arg1, arg2, tcs));

            return tcs;
        }

        public void Add(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
            => _newItems.Add((func, arg0, arg1, arg2, null));

        public void Release(PooledCompletionSource<TResult> tcs)
        {
            tcs.Reset();
            _sources.Push(tcs);
        }
        
        public override bool Update()
        {
            (_newItems, _items) = (_items, _newItems);
            _newItems.Clear();
            if (_items.Count == 0)
                return false;
            Monitor.Exit(owner._lock);
            foreach (var (func, arg0, arg1, arg2, tcs) in _items)
            {
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
            Monitor.Enter(owner._lock);
            return true;
        }
    }
    
    private readonly ILogger _logger;

    protected readonly object _lock = new();
    protected int _typeIndex;
    protected readonly PendingActionGroup _group;
    protected readonly PendingGroupBase[] _groups = new PendingGroupBase[256];

    protected readonly TContext _context;
    
    protected PendingActionScheduler(TContext context, ILogger logger)
    {
        _logger = logger;
        _group = new(this);
        _context = context;
    }
    
    public async ValueTask RunAsync(Action<TContext> action)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        
        lock (_lock)
        {
            tcs = _group.AddAsync(action);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            _group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T>(Action<TContext, T> action, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1>(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1, T2>(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1, arg2);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1, arg2);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1, T2, T3>(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1, arg2, arg3);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2, T3> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2, T3>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2, T3>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2, T3>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1, arg2, arg3);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public async ValueTask RunAsync<T0, T1, T2, T3, T4>(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1, arg2, arg3, arg4);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2, T3, T4> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3, T4>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2, T3, T4>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2, T3, T4>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2, T3, T4>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1, arg2, arg3, arg4);
        }

        await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }

    public async ValueTask<TResult> RunAsync<TResult>(Func<TContext, TResult> func)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(_context);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public async ValueTask<TResult> RunAsync<T, TResult>(Func<TContext, T, TResult> func, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(_context, arg);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public async ValueTask<TResult> RunAsync<T0, T1, TResult>(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(_context, arg0, arg1);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg0, arg1);
        }

        var result = await tcs.Task;
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public async ValueTask<TResult> RunAsync<T0, T1, T2, TResult>(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(_context, arg0, arg1, arg2);
        }
        
        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, T2, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, T2, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, T2, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg0, arg1, arg2);
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
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context);
            return;
        }
        
        lock (_lock)
        {
            _group.Add(action);
        }
    }
    
    public void Schedule<T>(Action<TContext, T> action, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg);
            return;
        }
        
        lock (_lock)
        {
            PendingActionGroup<T> group;
            var typeIndex = PendingActionGroup<T>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T>)_groups[typeIndex];
            }

            group.Add(action, arg);
        }
    }
    
    public void Schedule<T0, T1>(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1);
            return;
        }
        
        lock (_lock)
        {
            PendingActionGroup<T0, T1> group;
            var typeIndex = PendingActionGroup<T0, T1>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1>)_groups[typeIndex];
            }

            group.Add(action, arg0, arg1);
        }
    }
    
    public void Schedule<T0, T1, T2>(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1, arg2);
            return;
        }
        
        lock (_lock)
        {
            PendingActionGroup<T0, T1, T2> group;
            var typeIndex = PendingActionGroup<T0, T1, T2>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2>)_groups[typeIndex];
            }

            group.Add(action, arg0, arg1, arg2);
        }
    }
    
    public void Schedule<T0, T1, T2, T3>(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1, arg2, arg3);
            return;
        }
        
        lock (_lock)
        {
            PendingActionGroup<T0, T1, T2, T3> group;
            var typeIndex = PendingActionGroup<T0, T1, T2, T3>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2, T3>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2, T3>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2, T3>)_groups[typeIndex];
            }

            group.Add(action, arg0, arg1, arg2, arg3);
        }
    }
    
    public void Schedule<T0, T1, T2, T3, T4>(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1, arg2, arg3, arg4);
            return;
        }
        
        lock (_lock)
        {
            PendingActionGroup<T0, T1, T2, T3, T4> group;
            var typeIndex = PendingActionGroup<T0, T1, T2, T3, T4>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2, T3, T4>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2, T3, T4>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2, T3, T4>)_groups[typeIndex];
            }

            group.Add(action, arg0, arg1, arg2, arg3, arg4);
        }
    }

    public void Schedule<TResult>(Func<TContext, TResult> func)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            func(_context);
            return;
        }
        
        lock (_lock)
        {
            PendingFuncGroup<TResult> group;
            var typeIndex = PendingFuncGroup<TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<TResult>)_groups[typeIndex];
            }

            group.Add(func);
        }
    }
    
    public void Schedule<T, TResult>(Func<TContext, T, TResult> func, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            func(_context, arg);
            return;
        }
        
        lock (_lock)
        {
            PendingFuncGroup<T, TResult> group;
            var typeIndex = PendingFuncGroup<T, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T, TResult>)_groups[typeIndex];
            }

            group.Add(func, arg);
        }
    }
    
    public void Schedule<T0, T1, TResult>(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            func(_context, arg0, arg1);
            return;
        }
        
        lock (_lock)
        {
            PendingFuncGroup<T0, T1, TResult> group;
            var typeIndex = PendingFuncGroup<T0, T1, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, TResult>)_groups[typeIndex];
            }

            group.Add(func, arg0, arg1);
        }
    }
    
    public void Schedule<T0, T1, T2, TResult>(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            func(_context, arg0, arg1, arg2);
            return;
        }
        
        lock (_lock)
        {
            PendingFuncGroup<T0, T1, T2, TResult> group;
            var typeIndex = PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, T2, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, T2, TResult>)_groups[typeIndex];
            }

            group.Add(func, arg0, arg1, arg2);
        }
    }
    
    public void RunSynchronously(Action<TContext> action)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        
        lock (_lock)
        {
            tcs = _group.AddAsync(action);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            _group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T>(Action<TContext, T> action, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1>(Action<TContext, T0, T1> action, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1);
            return;
        }
        
        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1, T2>(Action<TContext, T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1, arg2);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1, arg2);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1, T2, T3>(Action<TContext, T0, T1, T2, T3> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1, arg2, arg3);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2, T3> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2, T3>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2, T3>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2, T3>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1, arg2, arg3);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }
    
    public void RunSynchronously<T0, T1, T2, T3, T4>(Action<TContext, T0, T1, T2, T3, T4> action, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            action(_context, arg0, arg1, arg2, arg3, arg4);
            return;
        }

        PooledCompletionSource<bool> tcs;
        PendingActionGroup<T0, T1, T2, T3, T4> group;
        
        lock (_lock)
        {
            var typeIndex = PendingActionGroup<T0, T1, T2, T3, T4>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingActionGroup<T0, T1, T2, T3, T4>.TypeIndex = typeIndex;
                group = new PendingActionGroup<T0, T1, T2, T3, T4>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingActionGroup<T0, T1, T2, T3, T4>)_groups[typeIndex];
            }

            tcs = group.AddAsync(action, arg0, arg1, arg2, arg3, arg4);
        }

        tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
    }

    public TResult RunSynchronously<TResult>(Func<TContext, TResult> func)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(_context);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func);
        }

        var result = tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public TResult RunSynchronously<T, TResult>(Func<TContext, T, TResult> func, T arg)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(_context, arg);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg);
        }

        var result = tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public TResult RunSynchronously<T0, T1, TResult>(Func<TContext, T0, T1, TResult> func, T0 arg0, T1 arg1)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(_context, arg0, arg1);
        }

        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg0, arg1);
        }

        var result = tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
    
    public TResult RunSynchronously<T0, T1, T2, TResult>(Func<TContext, T0, T1, T2, TResult> func, T0 arg0, T1 arg1, T2 arg2)
    {
        if (_thread == null)
            throw new InvalidOperationException("Cannot run action on a scheduled thread, no thread is currently set");

        if (_thread == Thread.CurrentThread)
        {
            return func(_context, arg0, arg1, arg2);
        }
        
        PooledCompletionSource<TResult> tcs;
        PendingFuncGroup<T0, T1, T2, TResult> group;
        
        lock (_lock)
        {
            var typeIndex = PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex;
            if (typeIndex < 0)
            {
                typeIndex = _typeIndex++;
                PendingFuncGroup<T0, T1, T2, TResult>.TypeIndex = typeIndex;
                group = new PendingFuncGroup<T0, T1, T2, TResult>(this);
                _groups[typeIndex] = group;
            }
            else
            {
                group = (PendingFuncGroup<T0, T1, T2, TResult>)_groups[typeIndex];
            }

            tcs = group.AddAsync(func, arg0, arg1, arg2);
        }

        var result = tcs.Task.GetAwaiter().GetResult();
        
        lock (_lock)
        {
            group.Release(tcs);
        }
        
        return result;
    }
}