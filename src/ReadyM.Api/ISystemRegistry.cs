using System;
using Friflo.Engine.ECS.Systems;

namespace ReadyM.Api;

public interface ISystemRegistry
{
    ArchetypeId RegisterArchetype(Action<EntityBuilder> populateComponents);
    ISystemRegistry AddSystem<T>() where T : BaseSystem, new();
    ISystemRegistry AddSystem<T>(T system) where T : BaseSystem;
}