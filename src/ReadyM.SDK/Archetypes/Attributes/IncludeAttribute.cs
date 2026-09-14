namespace ReadyM.SDK.Archetypes.Attributes;

/// <summary>
/// Include a mixin into this archetype.
/// </summary>
/// <param name="MixinType">The type of the mixin to include.</param>
/// <param name="Optional">Whether this mixin's fields may be missing.</param>
/// <param name="Prefix">A name to prefix the fields by.</param>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class IncludeAttribute(Type MixinType, bool Optional = false, string Prefix = "") : Attribute;