using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Mapping.Events;

internal class EventQueue(ILogger logger)
{
    private abstract class EntryBase
    {
        // empty
    }

    private abstract class EntryBase<TEvent> : EntryBase
    {
        public abstract void Invoke(in TEvent ev);
    }

    private class Entry<TEvent>(ILogger logger) : EntryBase<TEvent>
    {
        private Action<TEvent>? _handlers;

        public void RegisterHandler(Action<TEvent> handler)
        {
            _handlers += handler;
        }

        public override void Invoke(in TEvent ev)
        {
            if (_handlers == null)
            {
                logger.LogWarning("Invoking event of type {EventType} with no handlers registered", typeof(TEvent).FullName);
                return;
            }

            _handlers(ev);
        }
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

    private readonly Dictionary<Type, EntryBase> _handlersArg0 = new();
    private readonly Dictionary<(Type, Type), EntryBase> _handlersArg1 = new();
    private readonly Dictionary<(Type, Type, Type), EntryBase> _handlersArg2 = new();
    private readonly Dictionary<(Type, Type, Type, Type), EntryBase> _handlersArg3 = new();

    // NOTE: Each entry appears once in its event's list: a handler runs once per event, whichever key it is under.
    private readonly Dictionary<Type, List<EntryBase>> _entriesByEventType = new();

    private void AddEntry(Type eventType, EntryBase entry)
    {
        if (!_entriesByEventType.TryGetValue(eventType, out var entryList))
        {
            entryList = [];
            _entriesByEventType[eventType] = entryList;
        }

        entryList.Add(entry);
    }

    public void RegisterHandler<TEvent>(Action<TEvent> handler)
    {
        if (!_handlersArg0.TryGetValue(typeof(TEvent), out var entry))
        {
            entry = new Entry<TEvent>(logger);
            _handlersArg0[typeof(TEvent)] = entry;
            AddEntry(typeof(TEvent), entry);
        }

        ((Entry<TEvent>)entry).RegisterHandler(handler);
    }

    public void RegisterHandler<TEvent, TArg>(Action<TEvent, TArg> handler, TArg arg)
    {
        var key = (typeof(TEvent), typeof(TArg));
        if (!_handlersArg1.TryGetValue(key, out var entry))
        {
            entry = new Entry<TEvent, TArg>(logger);
            _handlersArg1[key] = entry;
            AddEntry(typeof(TEvent), entry);
        }

        ((Entry<TEvent, TArg>)entry).RegisterHandler(handler, arg);
    }

    public void RegisterHandler<TEvent, TArg0, TArg1>(Action<TEvent, TArg0, TArg1> handler, TArg0 arg0, TArg1 arg1)
    {
        var key = (typeof(TEvent), typeof(TArg0), typeof(TArg1));
        if (!_handlersArg2.TryGetValue(key, out var entry))
        {
            entry = new Entry<TEvent, TArg0, TArg1>(logger);
            _handlersArg2[key] = entry;
            AddEntry(typeof(TEvent), entry);
        }

        ((Entry<TEvent, TArg0, TArg1>)entry).RegisterHandler(handler, arg0, arg1);
    }

    public void RegisterHandler<TEvent, TArg0, TArg1, TArg2>(Action<TEvent, TArg0, TArg1, TArg2> handler, TArg0 arg0, TArg1 arg1, TArg2 arg2)
    {
        var key = (typeof(TEvent), typeof(TArg0), typeof(TArg1), typeof(TArg2));
        if (!_handlersArg3.TryGetValue(key, out var entry))
        {
            entry = new Entry<TEvent, TArg0, TArg1, TArg2>(logger);
            _handlersArg3[key] = entry;
            AddEntry(typeof(TEvent), entry);
        }

        ((Entry<TEvent, TArg0, TArg1, TArg2>)entry).RegisterHandler(handler, arg0, arg1, arg2);
    }

    public void Invoke<TEvent>(in TEvent ev)
    {
        if (_entriesByEventType.TryGetValue(typeof(TEvent), out var entryList))
        {
            foreach (var entry in entryList)
            {
                ((EntryBase<TEvent>)entry).Invoke(ev);
            }
        }
    }
}
