using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[ArchetypeMixin]
public readonly partial struct Loot
{
    public partial int Rarity { get; set; }
}

#region Generated

internal struct LootComponent : IComponent
{
    public int rarity;
}

public readonly partial struct Loot(EntityHandle handle) : IArchetypeMixin
{
    EntityHandle IArchetypeQueryable.Handle
    {
        get => handle;
        init => handle = value;
    }

    public ComponentSet Components => ComponentSet.Of<LootComponent>();

    public partial int Rarity
    {
        get => handle.GetComponent<LootComponent>().rarity;
        set => handle.GetComponent<LootComponent>().rarity = value;
    }
}

#endregion
