using System.ComponentModel;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Archetypes;

/// Common interface for archetypes and mixins.
public interface IArchetypeQueryable : IEntityShape
{
    /// <summary>Settable, unlike a view's: a shape is built around an entity rather than moved to one.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    new EntityHandle Handle { get; init; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    ComponentSet Components { get; }
}
