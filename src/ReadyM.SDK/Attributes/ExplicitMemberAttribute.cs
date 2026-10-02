namespace ReadyM.SDK.Attributes;

/// Names the member this property reads, for a shape whose component is given by
/// <see cref="ExplicitComponentAttribute"/> and whose names do not line up.
[AttributeUsage(AttributeTargets.Property)]
public sealed class ExplicitMemberAttribute(string member) : Attribute
{
    public string Member { get; } = member;
}
