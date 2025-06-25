using System;
using Friflo.Engine.ECS.Systems;

namespace ReadyM.Api;

public interface IModConfig
{
    ArchetypeId RegisterArchetype(Action<EntityBuilder> populateComponents);
    IModConfig AddSystem<T>() where T : BaseSystem, new();
    IModConfig AddSystem<T>(T system) where T : BaseSystem;
}