using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

internal abstract class IdComponentRegistryBase<TRegistry, TComponent>(
    IEnumerable<IComponentRegistrationBase<TRegistry, TComponent>> registrations)
    : ComponentRegistryBase<TRegistry, TComponent>(registrations)
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    private byte _componentCount;

    protected byte GetNextComponentId()
        => _componentCount;

    protected override TRegistry RegisterComponentImpl<T>(T defaultValue = default)
    {
        TakeId();
        return base.RegisterComponentImpl(defaultValue);
    }

    protected override TRegistry RegisterModComponentImpl(ModComponentInfo info, string typeFullName)
    {
        TakeId();
        return base.RegisterModComponentImpl(info, typeFullName);
    }

    private void TakeId()
    {
        if (_componentCount == byte.MaxValue)
        {
            throw new InvalidOperationException($"Cannot register more than {byte.MaxValue} components");
        }

        _componentCount++;
    }
}
