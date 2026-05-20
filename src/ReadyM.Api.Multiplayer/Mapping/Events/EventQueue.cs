using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Multiplayer.Mapping.Events;

internal class EventQueue(ILogger logger)
{
    private abstract class EntryBase
    {
        public abstract void Invoke(in object ev);
    }

    private abstract class EntryBase<TEvent> : EntryBase
    {
        public override void Invoke(in object ev)
        {
            if (ev is TEvent typedEv)
            {
                Invoke(typedEv);
            }
            else
            {
                throw new InvalidOperationException($"Invalid event type passed to entry. Expected {typeof(TEvent).FullName}, got {ev.GetType().FullName}");
            }
        }

        public abstract void Invoke(in TEvent ev);
    }

    private class Entry<TEvent, TArg>(ILogger logger) : EntryBase<TEvent>
    {
        private readonly List<(Action<TEvent, TArg>, TArg)> _handlers = new();

        public void RegisterHandler(Action<TEvent, TArg> handler, TArg arg)
        {
            _handlers.Add((handler, arg));
        }

        public override void Invoke(in TEvent ev)
        {
            if (_handlers.Count == 0)
            {
                logger.LogWarning("Invoking event of type {EventType} with no handlers registered", typeof(TEvent).FullName);
            }

            foreach (var (handler, arg) in _handlers)
            {
                handler(ev, arg);
            }
        }
    }
    
    private class OpaqueEntry<TArg>(ILogger logger) : EntryBase
    {
        private readonly List<(Action<object, TArg>, TArg)> _handlers = new();

        public void RegisterHandler(Action<object, TArg> handler, TArg arg)
        {
            _handlers.Add((handler, arg));
        }

        public override void Invoke(in object ev)
        {
            if (_handlers.Count == 0)
            {
                logger.LogWarning("Invoking event of type {EventType} with no handlers registered", ev.GetType().FullName);
            }

            foreach (var (handler, arg) in _handlers)
            {
                handler(ev, arg);
            }
        }
    }

    private class Entry<TEvent, TArg0, TArg1>(ILogger logger) : EntryBase<TEvent>
    {
        private readonly List<(Action<TEvent, TArg0, TArg1>, TArg0, TArg1)> _handlers = new();

        public void RegisterHandler(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
        {
            _handlers.Add((handler, arg0, arg1));
        }

        public override void Invoke(in TEvent ev)
        {
            if (_handlers.Count == 0)
            {
                logger.LogWarning("Invoking event of type {EventType} with no handlers registered", typeof(TEvent).FullName);
            }

            foreach (var (handler, arg0, arg1) in _handlers)
            {
                handler(ev, arg0, arg1);
            }
        }
    }

    private class Entry<TEvent, TArg0, TArg1, TArg2>(ILogger logger) : EntryBase<TEvent>
    {
        private readonly List<(Action<TEvent, TArg0, TArg1, TArg2>, TArg0, TArg1, TArg2)> _handlers = new();

        public void RegisterHandler(Action<TEvent, TArg0, TArg1, TArg2> handler, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            _handlers.Add((handler, arg0, arg1, arg2));
        }

        public override void Invoke(in TEvent ev)
        {
            if (_handlers.Count == 0)
            {
                logger.LogWarning("Invoking event of type {EventType} with no handlers registered", typeof(TEvent).FullName);
            }

            foreach (var (handler, arg0, arg1, arg2) in _handlers)
            {
                handler(ev, arg0, arg1, arg2);
            }
        }
    }

    private readonly Dictionary<Type, Delegate> _handlersArg0 = new();
    private readonly Dictionary<(Type, Type), EntryBase> _handlersArg1 = new();
    private readonly Dictionary<(Type, Type, Type), EntryBase> _handlersArg2 = new();
    private readonly Dictionary<(Type, Type, Type, Type), EntryBase> _handlersArg3 = new();

    private readonly Dictionary<Type, List<EntryBase>> _entriesByEventType = new();

    public void RegisterHandler<TEvent>(Action<TEvent> handler)
    {
        if (_handlersArg0.TryGetValue(typeof(TEvent), out var handlers))
        {
            handlers = Delegate.Combine(handlers, handler);
            _handlersArg0[typeof(TEvent)] = handlers;
        }
        else
        {
            _handlersArg0[typeof(TEvent)] = handler;
        }
    }

    public void RegisterHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
    {
        var key = (typeof(TEvent), typeof(TArg));
        if (!_handlersArg1.TryGetValue(key, out var entry))
        {
            entry = new Entry<TEvent, TArg>(logger);
            _handlersArg1[key] = entry;
        }

        ((Entry<TEvent, TArg>)entry).RegisterHandler(handler, arg);

        if (!_entriesByEventType.TryGetValue(typeof(TEvent), out var entryList))
        {
            entryList = new List<EntryBase>();
            _entriesByEventType[typeof(TEvent)] = entryList;
        }

        entryList.Add(entry);
    }

    public void RegisterOpaqueHandler<TArg>(Type eventType, Action<object, TArg> handler, TArg arg)
    {
        var key = (eventType, typeof(TArg));
        if (!_handlersArg1.TryGetValue(key, out var entry))
        {
            entry = new OpaqueEntry<TArg>(logger);
            _handlersArg1[key] = entry;
        }

        ((OpaqueEntry<TArg>)entry).RegisterHandler(handler, arg);

        if (!_entriesByEventType.TryGetValue(eventType, out var entryList))
        {
            entryList = new List<EntryBase>();
            _entriesByEventType[eventType] = entryList;
        }

        entryList.Add(entry);
    }

    public void RegisterHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
    {
        var key = (typeof(TEvent), typeof(TArg0), typeof(TArg1));
        if (!_handlersArg2.TryGetValue(key, out var entry))
        {
            entry = new Entry<TArg0, TArg1>(logger);
            _handlersArg2[key] = entry;
        }

        ((Entry<TEvent, TArg0, TArg1>)entry).RegisterHandler(handler, arg0, arg1);

        if (!_entriesByEventType.TryGetValue(typeof(TEvent), out var entryList))
        {
            entryList = new List<EntryBase>();
            _entriesByEventType[typeof(TEvent)] = entryList;
        }

        entryList.Add(entry);
    }

    public void RegisterHandler<TEvent, TArg0, TArg1, TArg2>(Action<TEvent, TArg0, TArg1, TArg2> handler, TArg0 arg0, TArg1 arg1, TArg2 arg2)
    {
        var key = (typeof(TEvent), typeof(TArg0), typeof(TArg1), typeof(TArg2));
        if (!_handlersArg3.TryGetValue(key, out var entry))
        {
            entry = new Entry<TArg0, TArg1, TArg2>(logger);
            _handlersArg3[key] = entry;
        }

        ((Entry<TEvent, TArg0, TArg1, TArg2>)entry).RegisterHandler(handler, arg0, arg1, arg2);

        if (!_entriesByEventType.TryGetValue(typeof(TEvent), out var entryList))
        {
            entryList = new List<EntryBase>();
            _entriesByEventType[typeof(TEvent)] = entryList;
        }

        entryList.Add(entry);
    }

    public void Invoke<TEvent>(in TEvent ev)
    {
        if (_handlersArg0.TryGetValue(typeof(TEvent), out var handlers))
        {
            var typedHandlers = (Action<TEvent>)handlers;
            typedHandlers?.Invoke(ev);
        }

        if (_entriesByEventType.TryGetValue(typeof(TEvent), out var entryList))
        {
            foreach (var entry in entryList)
            {
                ((EntryBase<TEvent>)entry).Invoke(ev);
            }
        }
    }
}