namespace ReadyM.SDK.Attributes;

/// Runs this method on an entity just before it is deleted.
[AttributeUsage(AttributeTargets.Method)]
public sealed class DeleteHandlerAttribute : Attribute
{
    public DeleteHandlerAttribute() { }

    public DeleteHandlerAttribute(Type shape) => Shape = shape;

    /// The shape watched from a service. Null on a shape's own handler, which watches itself.
    public Type? Shape { get; }
}
