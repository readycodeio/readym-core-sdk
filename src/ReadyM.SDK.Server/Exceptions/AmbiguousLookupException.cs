namespace ReadyM.SDK.Exceptions;

/// <summary>
/// A lookup that named no dimension found the key held in more than one: an area, a cell or a ref has a copy in each
/// dimension, so it is looked up with the dimension, <c>TryLookup(key, dimension, out shape)</c>.
/// </summary>
public sealed class AmbiguousLookupException(Type component, object? key)
    : InvalidOperationException(
        $"{component.Name} {key} is held in more than one dimension; look it up with its dimension.")
{
    /// <summary>The indexed component the key was looked up in.</summary>
    public Type Component { get; } = component;

    /// <summary>The key held in more than one dimension.</summary>
    public object? Key { get; } = key;
}
