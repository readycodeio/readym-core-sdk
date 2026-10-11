namespace ReadyM.Api.ECS.Registry;

internal interface ITypeRegistryBase<out TRegistry, out TComponent, out TEvent>
{
    // NOTE: Visitor pattern to handle generics without reflection. Components and events, in registration order.
    void Accept(ITypeRegistryCallbackBase<TRegistry, TComponent, TEvent> callback);
    TRegistry RegisterFilter(ITypeRegistryCallbackBase<TRegistry, TComponent, TEvent> filter);
}
