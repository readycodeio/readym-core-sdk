using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal abstract class ArchetypeComponentRegistryBase<TRegistry>(IEnumerable<IComponentRegistrationBase<TRegistry, IComponent>> registrations)
    : ComponentRegistryBase<TRegistry, IComponent>(registrations), IArchetypeComponentRegistryBase<TRegistry>
    where TRegistry : IComponentRegistryBase<TRegistry, IComponent>
{
    private readonly Dictionary<Type, Delegate?> _valueFactories = new();

    public void RegisterComponent<T>(Func<T>? valueFactory = null) where T : struct, IComponent
    {
        _valueFactories.Add(typeof(T), valueFactory);
        base.RegisterComponent<T>();
    }

    public bool TryGetValueFactory<T>([NotNullWhen(true)] out Func<T>? valueFactory)
        where T : struct, IComponent
    {
        valueFactory = (Func<T>?)_valueFactories[typeof(T)];
        return valueFactory is not null;
    }
}
