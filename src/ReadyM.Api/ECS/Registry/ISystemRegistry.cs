using System;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;

namespace ReadyM.Api.ECS.Registry;

public interface ISystemRegistry
{
    ArchetypeId RegisterArchetype(Action<EntityBuilder> populateComponents);
    ISystemRegistry AddSystem<T>() where T : BaseSystem, new();
    ISystemRegistry AddSystem<T>(T system) where T : BaseSystem;
}