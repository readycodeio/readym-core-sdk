using System.ComponentModel;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Archetypes;

/// <summary>
/// What an archetype or a mixin has in common: which entity it is, and what it is made of.
/// </summary>
public interface IArchetypeQueryable
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    EntityHandle Handle { get; init; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    ComponentSet Components { get; }
}
