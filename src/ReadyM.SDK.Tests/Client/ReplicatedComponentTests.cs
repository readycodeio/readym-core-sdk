
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// A shape marked [Replicated] gets a component that tracks its own changes, and every way of
/// writing it has to go through that tracking rather than round it.
public class ReplicatedComponentTests : ClientSdkTest
{
    private static bool DirtyOf(Rig rig) => EntityHandle.Of(rig).GetComponent<TelemetryComponent>().IsDirty;

    private static void ClearDirty(Rig rig) => EntityHandle.Of(rig).GetComponent<TelemetryComponent>().ClearDirty();

    [Fact]
    public void A_replicated_component_starts_clean()
    {
        var rig = Entities.Create<Rig>();

        Assert.False(DirtyOf(rig));
    }

    [Fact]
    public void A_write_through_the_shape_marks_it_dirty()
    {
        var rig = Entities.Create<Rig>();

        rig.Ticks = 5;

        Assert.True(DirtyOf(rig));
        Assert.Equal(5, rig.Ticks);
    }

    /// The chunk path writes straight into the component array, so it is the one that would slip
    /// past the tracking if the setter wrote the field.
    [Fact]
    public void A_write_through_a_chunk_marks_it_dirty()
    {
        var rig = Entities.Create<Rig>();

        ClearDirty(rig);

        Assert.False(DirtyOf(rig));

        foreach (var view in Entities.Query<Rig>())
            view.Ticks = 7;

        Assert.True(DirtyOf(rig));
        Assert.Equal(7, rig.Ticks);
    }

    /// Each value has its own bit, so clearing after one write does not hide the next.
    [Fact]
    public void Each_value_is_tracked_on_its_own()
    {
        var rig = Entities.Create<Rig>();

        rig.Ticks = 1;
        ClearDirty(rig);

        rig.Load = 2f;

        Assert.True(DirtyOf(rig));
        Assert.Equal(1, rig.Ticks);
        Assert.Equal(2f, rig.Load);
    }

    /// A shape that never asked to replicate keeps a plain component, with nothing to track.
    [Fact]
    public void A_local_shape_gets_no_tracking()
    {
        Assert.DoesNotContain(
            typeof(ClimateComponent).GetInterfaces(),
            contract => contract.Name == "INetworkedComponent");

        Assert.Contains(
            typeof(TelemetryComponent).GetInterfaces(),
            contract => contract.Name == "INetworkedComponent");
    }

    /// Nothing creates the collection here: the entity being created is what does that.
    private Rig WithMembers(params int[] values)
    {
        var rig = Entities.Create<Rig>();

        foreach (var value in values)
            rig.AddMembers(value);

        return rig;
    }

    /// The collection's own members reach the shape, so a mod adds one item rather than replacing
    /// the whole collection to change it.
    [Fact]
    public void A_native_collection_carries_its_members_onto_the_shape()
    {
        var rig = WithMembers(4, 9);

        Assert.Equal(2, rig.MembersCount);
        Assert.Equal(4, rig.GetMembers(0));
        Assert.True(rig.ContainsMembers(9));
        Assert.False(rig.ContainsMembers(5));

        rig.InsertMembers(0, 1);

        Assert.Equal(1, rig.GetMembers(0));
        Assert.Equal(3, rig.MembersCount);
        Assert.Equal(1, rig.RemoveAtMembers(0));

        rig.ClearMembers();

        Assert.Equal(0, rig.MembersCount);
    }

    /// Changing one item marks the component, the same as replacing the whole collection does.
    [Fact]
    public void Adding_to_a_native_collection_marks_it_dirty()
    {
        var rig = WithMembers();

        EntityHandle.Of(rig).GetComponent<RosterComponent>().ClearDirty();

        rig.AddMembers(7);

        Assert.True(EntityHandle.Of(rig).GetComponent<RosterComponent>().IsDirty);
        Assert.Equal(1, rig.MembersCount);
    }

    /// Creating the entity is what gives a native collection its memory, so a shape is usable the
    /// moment it exists and a mod never has to remember to allocate.
    [Fact]
    public void A_native_collection_is_ready_as_soon_as_the_entity_exists()
    {
        var rig = Entities.Create<Rig>();

        Assert.Equal(0, rig.MembersCount);

        rig.AddMembers(3);

        Assert.Equal(1, rig.MembersCount);
        Assert.Equal(3, rig.GetMembers(0));
    }
}
