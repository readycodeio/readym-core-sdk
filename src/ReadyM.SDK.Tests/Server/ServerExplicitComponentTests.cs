using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Shapes over storage the relay already owns, which is what a game SDK is built from.
/// </summary>
/// <remarks>
/// Nothing new crosses the boundary for these: the relay indexes its components by full type name
/// and the mod side resolves by the same name, so a component the relay registered is reachable
/// without the mod registering anything.
/// </remarks>
public class ServerExplicitComponentTests : ServerSdkTest
{
    [Fact]
    public void A_shape_reads_the_component_the_relay_owns()
    {
        var networked = Spawn<Networked>();

        Relay.Set(IdentityOf(networked), new RelayMetadataComponent(42, 7));

        Assert.Equal(42, networked.NetId);
        Assert.Equal(7, networked.Owner);
    }

    [Fact]
    public void A_write_through_the_shape_reaches_the_relay()
    {
        var networked = Spawn<Networked>();

        networked.Owner = 9;

        Assert.Equal(9, Relay.Get<RelayMetadataComponent>(IdentityOf(networked)).Owner);
    }

    /// The archetype has no marker, so its set is just the two components it names.
    [Fact]
    public void An_archetype_over_borrowed_storage_carries_no_marker()
    {
        var set = NetworkedAccessors.Components;

        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void A_borrowed_shape_is_walked_by_chunk_like_any_other()
    {
        for (var i = 0; i < 4; i++)
            Spawn<Networked>().Owner = i;

        Relay.ResetCounters();

        var owners = 0;

        foreach (var networked in Entities.Query<Networked>())
            owners += networked.Owner;

        Assert.Equal(6, owners);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    [Fact]
    public void A_write_through_a_chunk_reaches_the_relay()
    {
        var networked = Spawn<Networked>();

        foreach (var shape in Entities.Query<Networked>())
            shape.Owner = 33;

        Assert.Equal(33, Relay.Get<RelayMetadataComponent>(IdentityOf(networked)).Owner);
    }

    /// A mixin over borrowed storage is a shape in its own right.
    [Fact]
    public void A_borrowed_mixin_can_be_queried_on_its_own()
    {
        Spawn<Networked>().Owner = 5;

        var seen = 0;

        foreach (var metadata in Entities.Query<Metadata>())
            seen += metadata.Owner;

        Assert.Equal(5, seen);
    }
}
