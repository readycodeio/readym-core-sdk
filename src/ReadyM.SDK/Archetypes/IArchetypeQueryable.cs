using Friflo.Engine.ECS;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Archetypes;

public interface IArchetypeQueryable
{
    EntityHandle Handle { get; init; }
    ComponentTypes ComponentTypes { get; }
}