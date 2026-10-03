using System;
using System.Threading;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// What game events read (the ownership manager, the client state, ...): exactly the types the registered events
/// require, each got once at start-up and nothing else. Looking one up is an array index and a cast: no dictionary.
/// </summary>
public sealed class GameEventContextRegistry
{
    private sealed class Filling(GameEventContextRegistry registry, IGameEventContextSource source)
        : ITypeRegistryCallbackBase<IAllTypeRegistry, IComponent, IGameEvent>, IGameEventContextVisitor
    {
        private Type? _event;

        public void AcceptComponent<T>(IAllTypeRegistry types, T defaultValue = default)
            where T : struct, IComponent
        {
            // Components require nothing.
        }

        public void AcceptModComponent(IAllTypeRegistry types, ModComponentInfo info, string typeFullName)
        {
            // Components require nothing.
        }

        public void AcceptEvent<TEvent>(IAllTypeRegistry types)
            where TEvent : struct, IGameEvent
        {
            // NOTE: Boxes once per event type, at start-up; the requirements are written on the event by the generator.
            if ((object)default(TEvent) is not IGameEventRequiresContextBase requirer)
                return;

            _event = typeof(TEvent);
            requirer.AcceptContexts(this);
        }

        public void Accept<T>()
            where T : class
        {
            if (registry.Contains<T>())
                return;

            T context;
            try
            {
                context = source.Get<T>();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Game event {_event!.FullName} requires {typeof(T).FullName}, which could not be provided", e);
            }

            registry.Set(context);
        }
    }

    private static int _contextTypeCount;

    private static class ContextIndex<T>
        where T : class
    {
        public static readonly int Value = Interlocked.Increment(ref _contextTypeCount) - 1;
    }

    private object?[] _contexts = [];

    internal GameEventContextRegistry(IAllTypeRegistry types, IGameEventContextSource source)
    {
        types.Accept(new Filling(this, source));
    }

    public T GetContext<T>()
        where T : class
    {
        var index = ContextIndex<T>.Value;
        if (index < _contexts.Length && _contexts[index] is T context)
            return context;

        throw new InvalidOperationException($"No game event requires {typeof(T).FullName}, so the registry does not hold one");
    }

    private bool Contains<T>()
        where T : class
    {
        var index = ContextIndex<T>.Value;
        return index < _contexts.Length && _contexts[index] != null;
    }

    private void Set<T>(T context)
        where T : class
    {
        var index = ContextIndex<T>.Value;
        if (index >= _contexts.Length)
            Array.Resize(ref _contexts, index + 1);

        _contexts[index] = context;
    }
}
