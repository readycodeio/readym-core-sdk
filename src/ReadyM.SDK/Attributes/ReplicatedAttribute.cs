namespace ReadyM.SDK.Attributes;

/// Replicates this shape's values to the other side.
[AttributeUsage(AttributeTargets.Struct)]
public sealed class ReplicatedAttribute(Delivery delivery = Delivery.Reliable) : Attribute
{
    public Delivery Delivery { get; } = delivery;
}
