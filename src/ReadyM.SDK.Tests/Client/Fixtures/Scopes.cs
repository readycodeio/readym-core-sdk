using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// Stands in for a scope component the relay owns, indexed the way AreaScopeComponent is.
internal struct TestAreaScopeComponent : IIndexedComponent<int>
{
    public int AreaId;

    public int GetIndexedValue() => AreaId;
}

/// An archetype declaring itself a scope, which is all IScope asks for.
[Archetype]
[ExplicitComponent(typeof(TestAreaScopeComponent))]
public readonly partial struct TestArea : IScope
{
    public partial int AreaId { get; set; }
}

[ArchetypeMixin]
public readonly partial struct Patrol
{
    public partial int Route { get; set; }
}

[Archetype]
[Include(typeof(Health))]
[Include(typeof(Patrol))]
public readonly partial struct Guard;

[Archetype]
[Include(typeof(Health))]
public readonly partial struct Critter;
