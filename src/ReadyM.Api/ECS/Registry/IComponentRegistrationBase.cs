namespace ReadyM.Api.ECS.Registry;

public interface IComponentRegistrationBase<in TRegistry, TComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    void Register(TRegistry registry);
}