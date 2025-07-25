using System;
using Friflo.Engine.ECS.Systems;

namespace ReadyM.Api;

public sealed class SystemRegistry(Store world) : ISystemRegistry
{
    public ArchetypeId RegisterArchetype(Action<EntityBuilder> populateComponents)
    {
        return world.RegisterArchetype(populateComponents);
    }

    public ISystemRegistry AddSystem<T>() where T : BaseSystem, new()
    {
        world.SystemRoot.Add(new T());
        return this;
    }

    public ISystemRegistry AddSystem<T>(T system) where T : BaseSystem
    {
        world.SystemRoot.Add(system);
        return this;
    }
}