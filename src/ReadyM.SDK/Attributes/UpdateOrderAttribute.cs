namespace ReadyM.SDK.Attributes;

/// <summary>
/// Specifies when a service's update runs relative to the others.
/// </summary>
/// <remarks>
/// Before and After are always honoured, regardless of priorities. The priority only
/// decides between services that nothing orders against each other, and equal priorities fall back
/// to the type name, so a given set of mods always ticks in the same order.
/// </remarks>
/// <param name="priority">Lower runs earlier. 100 by default.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UpdateOrderAttribute(int priority = UpdateOrderAttribute.DefaultPriority) : Attribute
{
    public const int DefaultPriority = 100;

    public int Priority { get; } = priority;

    /// Services this one updates before.
    public Type[]? Before { get; set; }

    /// Services this one updates after.
    public Type[]? After { get; set; }
}
