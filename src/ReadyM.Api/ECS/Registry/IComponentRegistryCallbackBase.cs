namespace ReadyM.Api.ECS.Registry;

public interface IComponentRegistryCallbackBase<in TRegistry, in TComponent>
{
    void AcceptComponent<T>(TRegistry registry)
        where T : struct, TComponent;
    
    void AcceptComponent<T>(TRegistry registry, T defaultValue)
        where T : struct, TComponent;
}