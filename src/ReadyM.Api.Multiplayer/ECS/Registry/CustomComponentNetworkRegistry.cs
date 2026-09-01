using System;
using System.Collections.Generic;
using LiteNetLib;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class CustomComponentNetworkRegistry : INetworkedComponentRegistration, IComponentRegistry
{
    private readonly List<Type> _registeredComponentTypes = [];

    public void Register(INetworkedComponentRegistry registry)
    {
        foreach (var type in _registeredComponentTypes)
        {
            registry.RegisterComponent(type, DeliveryMethod.ReliableOrdered);
        }
    }

    public IComponentRegistry RegisterComponent<T>() where T : struct, INetworkedComponent
    {
        if (_registeredComponentTypes.Contains(typeof(T)))
            throw new InvalidOperationException($"Component type {typeof(T).FullName} is already registered.");

        _registeredComponentTypes.Add(typeof(T));

        return this;
    }
}