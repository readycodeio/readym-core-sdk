using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.ExternalMod;

/// An archetype another assembly includes, with an accessor of its own and a mixin of its own.
[Archetype]
[Include(typeof(Wallet))]
public readonly partial struct Merchant
{
    public partial int Reputation { get; set; }
}
