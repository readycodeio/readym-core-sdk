namespace ReadyM.SDK.Attributes;

/// Runs this method on an entity right after it is created.
[AttributeUsage(AttributeTargets.Method)]
public sealed class CreateHandlerAttribute : Attribute
{
    public CreateHandlerAttribute() { }

    public CreateHandlerAttribute(Type shape) => Shape = shape;

    public Type? Shape { get; }
}
