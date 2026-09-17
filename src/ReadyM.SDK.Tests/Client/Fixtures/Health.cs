using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[ArchetypeMixin]
public readonly partial struct Health
{
    public partial float Hp { get; set; }
    public partial float MaxHp { get; set; }
}
