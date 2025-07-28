using System;
using System.Collections.Generic;

namespace ReadyM.Api.ECS.Registry;

public interface IComponentRegistryBase<out TRegistry, TComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    IReadOnlyList<Type> ComponentTypes { get; }

    TRegistry RegisterComponent<T>()
        where T : struct, TComponent;
    
    // NOTE: Visitor pattern to handle generics without reflection.
    void Accept(IComponentRegistryCallbackBase<TRegistry, TComponent> callbackBase);
}
