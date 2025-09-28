namespace ReadyM.Api.ECS.Registry;

public interface IComponentRegistryBase<out TRegistry, out TComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    // NOTE: Visitor pattern to handle generics without reflection.
    void Accept(IComponentRegistryCallbackBase<TRegistry, TComponent> callbackBase);
}