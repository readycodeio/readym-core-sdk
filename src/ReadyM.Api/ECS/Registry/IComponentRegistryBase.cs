namespace ReadyM.Api.ECS.Registry;

internal interface IComponentRegistryBase<out TRegistry, out TComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    // NOTE: Visitor pattern to handle generics without reflection.
    void Accept(IComponentRegistryCallbackBase<TRegistry, TComponent> callback);
    TRegistry RegisterFilter(IComponentRegistryCallbackBase<TRegistry, TComponent> filter);
}
