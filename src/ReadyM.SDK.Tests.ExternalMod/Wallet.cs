using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.ExternalMod;

/// A mixin another assembly includes, so its component is never nameable from there.
[ArchetypeMixin]
public readonly partial struct Wallet
{
    public partial int Coins { get; set; }
}
