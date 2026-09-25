using Friflo.Engine.ECS;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// Stands in for a component a game core already owns: PascalCase members, one of them readonly.
internal struct CoreMetadataComponent : IComponent
{
    public readonly int NetId;
    public int Owner;
    public string Tag;

    public CoreMetadataComponent(int netId, int owner, string tag)
    {
        NetId = netId;
        Owner = owner;
        Tag = tag;
    }
}

/// The member a shape names does not have to match the field's name.
internal struct CoreScopeComponent : IComponent
{
    public int AreaIdentifier;
}

[ArchetypeMixin]
[ExplicitComponent(typeof(CoreMetadataComponent))]
public readonly partial struct Metadata
{
    public partial int NetId { get; }
    public partial int Owner { get; set; }
    public partial string Tag { get; set; }
}

[ArchetypeMixin]
[ExplicitComponent(typeof(CoreScopeComponent))]
public readonly partial struct Scope
{
    [ExplicitMember("AreaIdentifier")]
    public partial int Area { get; set; }
}

/// An archetype over storage it does not own, so it carries no marker.
[Archetype]
[Include(typeof(Metadata))]
[Include(typeof(Scope))]
public readonly partial struct NetworkedThing;

/// Mixes core-owned storage with a component of its own.
[Archetype]
[Include(typeof(Metadata))]
public readonly partial struct Tracked
{
    public partial int Ticks { get; set; }
}
