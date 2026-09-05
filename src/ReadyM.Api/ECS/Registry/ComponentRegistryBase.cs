using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

// NOTE: ATTENTION! ATTENTION! ATTENTION! Please keep this class clean. This is just a way to be able to register
// components using generic method calls and have the ability to carry that generic-ness into the callbacks via
// `Accept`. This is not a place to put any specific logic concerning any particular registry.
// If you're thinking about adding more code here 99% chance you want to do it in that specific registry NOT here.
// Please consult before making substantial changes here.
internal abstract class ComponentRegistryBase<TRegistry, TComponent> : IComponentRegistryBase<TRegistry, TComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    private readonly List<Action<IComponentRegistryCallbackBase<TRegistry, TComponent>>> _acceptCallbacks = [];
    private readonly List<IComponentRegistryCallbackBase<TRegistry, TComponent>> _filters = [];

    // NOTE: DO NOT use for id generation. There's a specialized `IdComponentRegistryBase` for that.
    private readonly List<Type> _componentTypes = [];

    // Registering a type twice is always a mistake, so it throws. Nothing deduplicates downstream: each
    // registration takes another component id out of a byte-wide space, and for a networked component another
    // one for its change component. It used to pass silently and only show up as ids running short or as two
    // entries for one type drifting apart.
    private readonly HashSet<Type> _registeredTypes = [];
    private readonly HashSet<string> _registeredModComponents = [];

    public bool HasComponents
        => _componentTypes.Count > 0;

    public IEnumerable<Type> ComponentTypes => _componentTypes;

    // Registration is a construction-phase activity. Once the constructor returns, a fully built registry
    // exists and anyone holding one is entitled to assume the component set is settled, so a later
    // registration has to fail rather than land somewhere nothing will read.
    private bool _sealed;

    protected ComponentRegistryBase(IEnumerable<IComponentRegistrationBase<TRegistry, TComponent>> registrations)
    {
        var registry = (TRegistry)(object)this;
        foreach (var registration in registrations)
        {
            registration.Register(registry);
        }

        _sealed = true;
    }

    private void ThrowIfSealed(string what)
    {
        if (!_sealed)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{typeof(TRegistry).Name} finished construction, so {what} cannot be registered any more. "
            + "Components are registered by the registrations the registry is built from, and anything "
            + "holding the finished registry is entitled to assume the set is complete.");
    }

    /// <summary>
    /// Collects a component compiled into this build. Records a deferred action and does no work itself, so
    /// what an acceptor sees is decided by when the acceptor runs, not by when the registration happened.
    /// <see cref="RegisterModComponentImpl"/> is the mod-defined counterpart.
    /// </summary>
    protected virtual TRegistry RegisterComponentImpl<T>(T defaultValue = default)
        where T : struct, TComponent
    {
        if (defaultValue is IDisposable && !defaultValue.Equals(default(T)))
        {
            throw new InvalidOperationException(
                $"Component {typeof(T).Name} was registered with a constructed default. LIKELY it contains allocated " +
                "native buffers that every entity would then share. Due to how native buffers work, this would not " +
                "just cause aliasing issues but also likely cause hard-to-debug crashes. Register the type without a " +
                "value and let each entity allocate its own on first write");
        }

        ThrowIfSealed(typeof(T).Name);

        if (!_registeredTypes.Add(typeof(T)))
        {
            throw new InvalidOperationException(
                $"{typeof(T).FullName} is already registered on {typeof(TRegistry).Name}. A component type "
                + "belongs to exactly one registration: shared components to the default one, a game's own to "
                + "that game's, and mod-declared ones to the mod registration.");
        }

        _componentTypes.Add(typeof(T));

        var accept = new Action<IComponentRegistryCallbackBase<TRegistry, TComponent>>(callback =>
        {
            callback.AcceptComponent((TRegistry)(object)this, defaultValue);
        });
        _acceptCallbacks.Add(accept);

        foreach (var filter in _filters)
        {
            accept(filter);
        }

        return (TRegistry)(object)this;
    }

    /// <summary>
    /// Collects a mod-defined component. Mirrors <see cref="RegisterComponentImpl{T}"/> exactly: it records a
    /// deferred action and does no work, so a mod component reaches every acceptor at the same point in the
    /// build as a native one rather than being acted on the moment it arrives.
    /// </summary>
    protected virtual TRegistry RegisterModComponentImpl(ModComponentInfo info, string typeFullName)
    {
        ThrowIfSealed(typeFullName);

        if (!string.IsNullOrEmpty(typeFullName) && !_registeredModComponents.Add(typeFullName))
        {
            throw new InvalidOperationException(
                $"Mod component '{typeFullName}' is already registered on {typeof(TRegistry).Name}. Two mods "
                + "declaring the same component type, or one mod declaring it twice, would each get their own "
                + "component id for what the schema holds once.");
        }

        if (string.IsNullOrEmpty(typeFullName))
        {
            throw new ArgumentException(
                "A mod component must be registered under its full type name. It has no managed type, so the "
                + "name is the only thing that identifies it.", nameof(typeFullName));
        }

        var accept = new Action<IComponentRegistryCallbackBase<TRegistry, TComponent>>(callback =>
        {
            callback.AcceptModComponent((TRegistry)(object)this, info, typeFullName);
        });
        _acceptCallbacks.Add(accept);

        foreach (var filter in _filters)
        {
            accept(filter);
        }

        return (TRegistry)(object)this;
    }

    protected TRegistry RegisterComponentImpl(Type componentType, TComponent? defaultValue = default)
    {
        if (!typeof(TComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"Type {componentType.FullName} is not assignable to {typeof(TComponent).FullName}");
        if (!componentType.IsValueType)
            throw new ArgumentException($"Type {componentType.FullName} is not a value type");

        var method = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m => m is { Name: nameof(RegisterComponentImpl), IsGenericMethodDefinition: true });
        if (method == null)
            throw new InvalidOperationException($"Could not find RegisterComponent method for type {componentType.FullName}");
        method = method.MakeGenericMethod(componentType);

        if (defaultValue == null)
            defaultValue = (TComponent)Activator.CreateInstance(componentType)!;
        method.Invoke(this, [defaultValue]);
        return (TRegistry)(object)this;
    }

    public TRegistry RegisterFilter(IComponentRegistryCallbackBase<TRegistry, TComponent> filter)
    {
        // NOTE: Order matters
        _filters.Add(filter);
        foreach (var accept in _acceptCallbacks.ToList())
        {
            accept(filter);
        }

        return (TRegistry)(object)this;
    }

    public void Accept(IComponentRegistryCallbackBase<TRegistry, TComponent> callback)
    {
        foreach (var accept in _acceptCallbacks)
        {
            accept(callback);
        }
    }
}
