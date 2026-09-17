using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[ArchetypeMixin]
public readonly partial struct Loot
{
    public partial int Rarity { get; set; }
}
