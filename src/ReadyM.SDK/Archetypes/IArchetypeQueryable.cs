using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Archetypes;

public interface IArchetypeQueryable
{
    EntityHandle Handle { get; init; }
    
    ComponentSet Components { get; }
}
