namespace ReadyM.SDK.Attributes;

/// Exposes a collection of the explicit component on this shape, by forwarding the methods the
/// component already offers for it. Use it instead of naming the backing field, so that writes keep
/// going through the component and stay tracked for replication.
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class ExplicitCollectionAttribute(string member) : Attribute
{
    public string Member { get; } = member;
}
