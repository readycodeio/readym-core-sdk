using ProtocolRelayMode = ReadyM.Api.Multiplayer.Protocol.Enums.RelayMode;
using RelayMode = ReadyM.SDK.Attributes.RelayMode;

namespace ReadyM.SDK.Tests.Rpc;

/// <summary>Generated code casts the mod-facing RelayMode to the wire one by value, so the two must match exactly.</summary>
public class RelayModeTests
{
    [Fact]
    public void Both_have_the_same_underlying_type()
        => Assert.Equal(Enum.GetUnderlyingType(typeof(ProtocolRelayMode)), Enum.GetUnderlyingType(typeof(RelayMode)));

    [Fact]
    public void Both_have_the_same_names_with_the_same_values()
    {
        var sdk = Enum.GetValues<RelayMode>().ToDictionary(v => v.ToString(), v => (byte)v);
        var protocol = Enum.GetValues<ProtocolRelayMode>().ToDictionary(v => v.ToString(), v => (byte)v);

        Assert.Equal(protocol.OrderBy(p => p.Key), sdk.OrderBy(p => p.Key));
    }
}
