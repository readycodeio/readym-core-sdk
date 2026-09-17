namespace ReadyM.SDK.Attributes;

/// Adds this mixin to every entity created as the named archetype, without changing what that
/// archetype matches. A query for the archetype keeps finding entities that do not carry the mixin,
/// so one mod extending an archetype never narrows another mod's queries.
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class ExtendsAttribute(Type archetype) : Attribute
{
    public Type Archetype { get; } = archetype;
}
