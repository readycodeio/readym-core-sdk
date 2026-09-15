using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Tests.ExternalMod;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// Includes a mixin and an archetype declared in another assembly, whose components this assembly
/// cannot name.
[Archetype]
[Include(typeof(Placement))]
[Include(typeof(Wallet))]
[IncludeArchetype(typeof(Merchant))]
public readonly partial struct Trader
{
    public partial int Haggling { get; set; }
}
