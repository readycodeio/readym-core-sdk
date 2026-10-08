using System;
using System.Collections.Generic;

namespace ReadyM.Api.ECS.Registry;

// NOTE: One id counter for components and events, so a type registered as an event keeps the id it would get as a
// component.
internal abstract class IdTypeRegistryBase<TRegistry, TComponent, TEvent>(
    IEnumerable<ITypeRegistrationBase<TRegistry>> registrations)
    : TypeRegistryBase<TRegistry, TComponent, TEvent>(registrations)
{
    private byte _count;

    protected byte GetNextId()
        => _count;

    protected override TRegistry RegisterComponentImpl<T>(T defaultValue = default)
    {
        SkipId();
        return base.RegisterComponentImpl(defaultValue);
    }

    protected override TRegistry RegisterEventImpl<T>()
    {
        SkipId();
        return base.RegisterEventImpl<T>();
    }

    protected void SkipId()
    {
        if (_count == byte.MaxValue)
        {
            throw new InvalidOperationException($"Cannot register more than {byte.MaxValue} components and events");
        }

        _count++;
    }
}
