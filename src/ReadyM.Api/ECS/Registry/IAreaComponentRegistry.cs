using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

internal interface IAreaComponentRegistry : IComponentRegistryBase<IAreaComponentRegistry, IComponent>
{
    void RegisterComponent<T>(T defaultValue = default)
        where T : struct, IComponent;
}
