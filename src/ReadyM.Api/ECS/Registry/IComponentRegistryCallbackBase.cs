namespace ReadyM.Api.ECS.Registry;

public interface IComponentRegistryCallbackBase<in TRegistry, in TComponent>
{
    void AcceptComponent<T>(TRegistry registry)
        where T : struct, TComponent;
}