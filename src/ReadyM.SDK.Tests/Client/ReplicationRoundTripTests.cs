
using LiteNetLib.Utils;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// A replicated shape written on one side and read on another. The generated component is what
/// crosses, so this is the whole of what [Replicated] buys once the wire is out of the picture.
public class ReplicationRoundTripTests : ClientSdkTest
{
    private static byte[] Snapshot<T>(in T component) where T : struct, global::ReadyM.Api.Multiplayer.ECS.Components.INetworkedComponent
    {
        var writer = new NetDataWriter();

        component.Serialize(writer);

        return writer.CopyData();
    }

    private static byte[] Delta<T>(in T component) where T : struct, global::ReadyM.Api.Multiplayer.ECS.Components.INetworkedComponent
    {
        var writer = new NetDataWriter();

        component.WriteDelta(writer);

        return writer.CopyData();
    }

    [Fact]
    public void A_snapshot_carries_every_value_across()
    {
        var rig = Entities.Create<Rig>();

        rig.Ticks = 11;
        rig.Load = 2.5f;

        var sent = Snapshot(EntityHandle.Of(rig).GetComponent<TelemetryComponent>());

        var received = default(TelemetryComponent);
        received.Deserialize(new NetDataReader(sent));

        Assert.Equal(11, received.Ticks);
        Assert.Equal(2.5f, received.Load);
    }

    /// A delta carries only what changed, which is what the dirty mask is for.
    [Fact]
    public void A_delta_carries_only_what_changed()
    {
        var rig = Entities.Create<Rig>();

        rig.Ticks = 1;
        rig.Load = 1f;

        var received = default(TelemetryComponent);
        received.Deserialize(new NetDataReader(Snapshot(EntityHandle.Of(rig).GetComponent<TelemetryComponent>())));

        // Moved apart from the sender, so a delta that carried everything would overwrite it and
        // a delta that carried only the change would leave it alone.
        received.Load = 99f;

        EntityHandle.Of(rig).GetComponent<TelemetryComponent>().ClearDirty();

        rig.Ticks = 7;

        received.ReadDelta(new NetDataReader(Delta(EntityHandle.Of(rig).GetComponent<TelemetryComponent>())));

        Assert.Equal(7, received.Ticks);
        Assert.Equal(99f, received.Load);
    }

    /// Sending is what clears the mark, so the next delta carries the next change and nothing else.
    [Fact]
    public void Clearing_after_a_send_leaves_nothing_to_send()
    {
        var rig = Entities.Create<Rig>();

        rig.Ticks = 4;

        Assert.True(EntityHandle.Of(rig).GetComponent<TelemetryComponent>().IsDirty);

        EntityHandle.Of(rig).GetComponent<TelemetryComponent>().ClearDirty();

        Assert.False(EntityHandle.Of(rig).GetComponent<TelemetryComponent>().IsDirty);
    }

    /// The collection travels by value, so the far side holds its own copy rather than a view of
    /// memory it does not own.
    [Fact]
    public void A_native_collection_crosses_intact()
    {
        var rig = Entities.Create<Rig>();

        rig.AddMembers(3);
        rig.AddMembers(8);

        var sent = Snapshot(EntityHandle.Of(rig).GetComponent<RosterComponent>());

        var received = default(RosterComponent);
        received.Deserialize(new NetDataReader(sent));

        Assert.Equal(2, received.MembersCount);
        Assert.Equal(3, received.GetMembers(0));
        Assert.Equal(8, received.GetMembers(1));
    }
}
