using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

public interface IPlayerComponentRegistry : IComponentRegistryBase<IPlayerComponentRegistry, IComponent>
{
    void RegisterComponent<T>(T defaultValue = default)
        where T : struct, IComponent;
}
