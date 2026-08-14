using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

internal interface IWorldComponentRegistry : IComponentRegistryBase<IWorldComponentRegistry, IComponent>
{
    void RegisterComponent<T>(T defaultValue = default)
        where T : struct, IComponent;
}
