using System.ComponentModel;

namespace ReadyM.SDK.Attributes.Generated;

/// Marks a generated member that changes a replicated collection, naming the token standing for it.
/// <remarks>
/// A collection has no setter to recognise, so what a value carries in its declaration this carries
/// on the member. It is what tells a call that changes one from a call that reads it.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Method)]
public sealed class ChangesAttribute(string collection) : Attribute
{
    public string Collection { get; } = collection;
}
