using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <summary>
/// The dimension an entity is in, or <see cref="DimensionId.Shared"/> when every dimension shares it. Server-only: not
/// networked, not savable. Every server archetype carries it from registration. Scope entities get their dimension when they are
/// provided. Every other entity has it derived from its scope by the server's membership observer, and nothing else
/// writes it.
/// </summary>
internal struct DimensionComponent(DimensionId dimension) : IIndexedComponent<DimensionId>
{
    public DimensionId Dimension = dimension;

    public readonly DimensionId GetIndexedValue() => Dimension;
}
