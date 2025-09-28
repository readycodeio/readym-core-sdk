using System;
using System.Collections.Generic;

namespace ReadyM.Api.ECS.Registry;

public abstract class ComponentRegistryBase<TRegistry, TComponent> : IComponentRegistryBase<TRegistry, TComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    private readonly List<Action<IComponentRegistryCallbackBase<TRegistry, TComponent>>> _acceptCallbacks = new();
    private readonly List<Type> _componentTypes = new();
    
    public IReadOnlyList<Type> ComponentTypes
        => _componentTypes;

    protected ComponentRegistryBase(IEnumerable<IComponentRegistrationBase<TRegistry, TComponent>> registrations)
    {
        var registry = (TRegistry)(object)this;
        foreach (var registration in registrations)
        {
            registration.Register(registry);
        }
    }
    
    protected TRegistry RegisterComponent<T>(T defaultValue = default)
        where T : struct, TComponent
    {
        _componentTypes.Add(typeof(T));
        _acceptCallbacks.Add(callback =>
        {
            callback.AcceptComponent((TRegistry)(object)this, defaultValue);
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