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

        _componentTypes++;
        _acceptCallbacks.Add(callback =>
        {
            callback.AcceptComponent((TRegistry)(object)this, defaultValue);
        });

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