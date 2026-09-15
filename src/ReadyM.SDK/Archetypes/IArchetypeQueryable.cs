using System.ComponentModel;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Archetypes;

public interface IArchetypeQueryable
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    EntityHandle Handle { get; init; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    ComponentSet Components { get; }
}
