using System.ComponentModel;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Archetypes;

/// Common interface for archetypes and mixins.
public interface IArchetypeQueryable
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    EntityHandle Handle { get; init; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    ComponentSet Components { get; }
}
