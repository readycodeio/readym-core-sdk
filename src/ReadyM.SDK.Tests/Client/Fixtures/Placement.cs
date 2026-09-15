using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[ArchetypeMixin]
public readonly partial struct Placement
{
    public partial float X { get; set; }
    public partial float Y { get; set; }
}
