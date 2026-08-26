using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

    public bool HasComponents
        => _componentTypes.Count > 0;

    public IEnumerable<Type> ComponentTypes => _componentTypes;

    protected ComponentRegistryBase(IEnumerable<IComponentRegistrationBase<TRegistry, TComponent>> registrations)
    {
        var registry = (TRegistry)(object)this;
        foreach (var registration in registrations)
        {
            registration.Register(registry);
        }
    }

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
