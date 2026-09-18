namespace ReadyM.SDK.Attributes;

/// Stores this shape's values in a component that already exists, instead of one generated for it.
/// Used for backwards compatibility with componens in SDK 0.x.
[AttributeUsage(AttributeTargets.Struct)]
internal sealed class ExplicitComponentAttribute(Type component) : Attribute
{
    public Type Component { get; } = component;
}
