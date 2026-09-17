namespace ReadyM.SDK.Attributes;

/// <summary>
/// Include all of another archetype's fields into this archetype.
/// </summary>
/// <param name="ArchetypeType">The type of the archetype to include.</param>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class IncludeArchetypeAttribute(Type ArchetypeType) : Attribute;