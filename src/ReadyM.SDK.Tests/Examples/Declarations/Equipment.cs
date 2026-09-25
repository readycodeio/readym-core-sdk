using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[ArchetypeMixin]
public readonly partial struct Equipment
{
    public partial string Weapon { get; set; }
}
