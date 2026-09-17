using System.ComponentModel;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Archetypes;

/// <summary>
/// Anything that names one entity: an archetype, a mixin, or the chunk view of either.
/// </summary>
public interface IEntityShape
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    EntityHandle Handle { get; }
}
