namespace ReadyM.Api.ECS.Registry;

internal interface IComponentRegistryCallbackBase<in TRegistry, in TComponent>
{
    void AcceptComponent<T>(TRegistry registry, T defaultValue = default)
        where T : struct, TComponent;
}