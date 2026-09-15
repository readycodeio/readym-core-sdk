using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[ArchetypeMixin]
public readonly partial struct Vitals
{
    public partial float Hp { get; set; }
    public partial float MaxHp { get; set; }
    public partial bool IsDead { get; } // read-only accessor: no setter generated
}
