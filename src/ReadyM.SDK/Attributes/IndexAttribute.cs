namespace ReadyM.SDK.Attributes;

/// Indexes the shape by this property, so an entity can be found by its value. One property per
/// shape may carry it, and writing it keeps the index current.
[AttributeUsage(AttributeTargets.Property)]
public sealed class IndexAttribute : Attribute;
