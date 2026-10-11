namespace ReadyM.Api.ECS.Registry;

internal interface IComponentRegistrationBase<in TRegistry, TComponent> : ITypeRegistrationBase<TRegistry>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    // empty
}
