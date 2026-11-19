namespace ReadyM.SDK.Attributes;

/// <summary>
/// Indexes the shape by this property, so an entity can be found by its value. One property per
/// shape may carry it, and writing it keeps the index current.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IndexAttribute : Attribute
{
    /// <summary>
    /// The value is held once in the whole world (a player id), not once per dimension (an area): the shape can then
    /// be looked up with no dimension.
    /// </summary>
    public bool HeldOnce { get; set; }
}
