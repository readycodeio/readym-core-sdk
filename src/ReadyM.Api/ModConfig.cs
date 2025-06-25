using System;
using Friflo.Engine.ECS.Systems;

namespace ReadyM.Api;

internal sealed class ModConfig(IEcs mod) : IModConfig
{
    public ArchetypeId RegisterArchetype(Action<EntityBuilder> populateComponents)
    {
        return mod.World.RegisterArchetype(populateComponents);
    }

    public IModConfig AddSystem<T>() where T : BaseSystem, new()
    {
        mod.World.SystemRoot.Add(new T());
        return this;
    }

    public IModConfig AddSystem<T>(T system) where T : BaseSystem
    {
        mod.World.SystemRoot.Add(system);
        return this;
    }
}