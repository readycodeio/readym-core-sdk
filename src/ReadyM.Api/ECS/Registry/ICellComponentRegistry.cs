using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

internal interface ICellComponentRegistry : IComponentRegistryBase<ICellComponentRegistry, IComponent>
{
    void RegisterComponent<T>(T defaultValue = default)
        where T : struct, IComponent;
}
