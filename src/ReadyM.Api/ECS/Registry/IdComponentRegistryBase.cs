using System;
using System.Collections.Generic;

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
        if (_componentCount == byte.MaxValue)
        {
            throw new InvalidOperationException($"Cannot register more than {byte.MaxValue} components");
        }
        _componentCount++;

        return base.RegisterComponentImpl(defaultValue);
    }

    protected void SkipId()
    {
        if (_componentCount == byte.MaxValue)
        {
            throw new InvalidOperationException($"Cannot register more than {byte.MaxValue} components");
        }

        _componentCount++;
    }
}
