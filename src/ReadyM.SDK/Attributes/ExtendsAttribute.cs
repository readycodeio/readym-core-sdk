namespace ReadyM.SDK.Attributes;

/// Adds this mixin to every entity created as the named archetype.
/// <param name="archetype">The archetype to add this mixin to.</param>
/// <param name="prefix">
/// Put in front of each member this mixin adds to the archetype, for a name that would otherwise
/// read oddly or clash with another mod's. <c>Local</c> turns <c>Stamina</c> into
/// <c>LocalStamina</c>.
/// </param>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class ExtendsAttribute(Type archetype, string prefix = "") : Attribute
{
    public Type Archetype { get; } = archetype;

    public string Prefix { get; } = prefix;
}
