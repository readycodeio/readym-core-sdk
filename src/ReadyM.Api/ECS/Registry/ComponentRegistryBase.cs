using System;
using System.Collections.Generic;

namespace ReadyM.Api.ECS.Registry;

internal abstract class ComponentRegistryBase<TRegistry, TComponent> : IComponentRegistryBase<TRegistry, TComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    private readonly List<Action<IComponentRegistryCallbackBase<TRegistry, TComponent>>> _acceptCallbacks = [];
    private byte _componentTypes;

    protected ComponentRegistryBase(IEnumerable<IComponentRegistrationBase<TRegistry, TComponent>> registrations)
    {
        var registry = (TRegistry)(object)this;
        foreach (var registration in registrations)
        {
            registration.Register(registry);
        }
    }

    protected byte GetNextComponentId()
    {
        return _componentTypes;
    }

    public virtual TRegistry RegisterComponent<T>(T defaultValue = default)
        where T : struct, TComponent
    {
        if (_componentTypes == byte.MaxValue)
        {
            throw new InvalidOperationException($"Cannot register more than {byte.MaxValue} components");
        }

        if (defaultValue is IDisposable && !defaultValue.Equals(default(T)))
        {
            throw new InvalidOperationException(
                $"Component {typeof(T).Name} was registered with a constructed default, whose native buffers "
                + "every entity would then share. Register the type without a value and let each entity allocate "
                + "its own on first write");
        }

        _componentTypes++;
        _acceptCallbacks.Add(callback =>
        {
            callback.AcceptComponent((TRegistry)(object)this, defaultValue);
        });

        return (TRegistry)(object)this;
    }

    public virtual TRegistry RegisterComponent(Type componentType, TComponent defaultValue)
    {
        if (!typeof(TComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"Type {componentType.FullName} is not assignable to {typeof(TComponent).FullName}");
        if (!componentType.IsValueType)
            throw new ArgumentException($"Type {componentType.FullName} is not a value type");

        var method = typeof(ComponentRegistryBase<TRegistry, TComponent>).GetMethod(nameof(RegisterComponent), [componentType]);
        if (method == null)
            throw new InvalidOperationException($"Could not find RegisterComponent method for type {componentType.FullName}");

        method.Invoke(this, [defaultValue]);
        return (TRegistry)(object)this;
    }

    protected TRegistry RegisterWithoutCallbacks()
    {
        if (_componentTypes == byte.MaxValue)
        {
            throw new InvalidOperationException($"Cannot register more than {byte.MaxValue} components");
        }

        _componentTypes++;
        _acceptCallbacks.Add(callback =>
        {
            // do nothing
        });

        return (TRegistry)(object)this;
    }

    public void Accept(IComponentRegistryCallbackBase<TRegistry, TComponent> callbackBase)
    {
        foreach (var acceptCallbacks in _acceptCallbacks)
        {
            acceptCallbacks(callbackBase);
        }
    }
}
