namespace ReadyM.SDK.Attributes;

public enum MappingPolicy
{
    Owner,
    Server,
    World
}

/// <summary>
/// Declares a struct as an archetype mixin.
/// </summary>
/// <param name="MappingPolicy">How the fields of this mixin are treated in the ownership model.</param>
[AttributeUsage(AttributeTargets.Struct)]
public class ArchetypeMixinAttribute(MappingPolicy MappingPolicy = MappingPolicy.Owner) : Attribute;