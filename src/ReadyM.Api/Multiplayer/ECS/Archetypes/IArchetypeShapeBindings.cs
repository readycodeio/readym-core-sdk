using System.Collections.Generic;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

internal interface IArchetypeShapeBindings
{
    IEnumerable<(string Shape, ArchetypeId Archetype)> Bindings { get; }
}
