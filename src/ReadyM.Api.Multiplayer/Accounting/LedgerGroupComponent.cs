using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Marks an entity something is owed about, naming the group of entities that are owed the same.</summary>
internal struct LedgerGroupComponent(int group) : IIndexedComponent<int>
{
    public int Group = group;

    public int GetIndexedValue()
        => Group;
}
