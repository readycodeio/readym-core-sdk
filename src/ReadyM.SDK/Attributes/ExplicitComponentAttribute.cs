namespace ReadyM.SDK.Attributes;

/// Stores this shape's values in a component that already exists, instead of one generated for it.
/// The type must be a struct implementing <c>Friflo.Engine.ECS.IComponent</c>, and each of the
/// shape's properties is matched to a field or property of the same name, ignoring case.
[AttributeUsage(AttributeTargets.Struct)]
public sealed class ExplicitComponentAttribute(Type component) : Attribute
{
    public Type Component { get; } = component;
}
