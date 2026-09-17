using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entity;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Tests.Client.Fixtures;
using FrifloEntity = Friflo.Engine.ECS.Entity;

namespace ReadyM.SDK.Tests.Client;

/// Shapes whose storage is a component that already existed, rather than one generated for them.
public class ExplicitComponentTests : ClientSdkTest
{
    private FrifloEntity Raw(NetworkedThing thing) => Store.GetEntityByRawEntity(EntityHandle.Of(thing).RawEntity);

    [Fact]
    public void The_shape_is_backed_by_the_component_that_already_existed()
    {
        var thing = Entities.Create<NetworkedThing>();

        Assert.True(Raw(thing).HasComponent<CoreMetadataComponent>());
    }

    [Fact]
    public void A_write_through_the_shape_lands_in_that_component()
    {
        var thing = Entities.Create<NetworkedThing>();

        thing.Owner = 7;
        thing.Tag = "guard";

        Assert.Equal(7, Raw(thing).GetComponent<CoreMetadataComponent>().Owner);
        Assert.Equal("guard", Raw(thing).GetComponent<CoreMetadataComponent>().Tag);
    }

    /// The core writes its own component; the shape reads exactly that.
    [Fact]
    public void A_value_written_as_the_component_is_read_through_the_shape()
    {
        var thing = Entities.Create<NetworkedThing>();

        Raw(thing).GetComponent<CoreMetadataComponent>() = new CoreMetadataComponent(42, 3, "core");

        Assert.Equal(42, thing.NetId);
        Assert.Equal(3, thing.Owner);
        Assert.Equal("core", thing.Tag);
    }

    [Fact]
    public void A_member_named_by_the_author_is_the_one_written()
    {
        var thing = Entities.Create<NetworkedThing>();

        thing.Area = 9;

        Assert.Equal(9, Raw(thing).GetComponent<CoreScopeComponent>().AreaIdentifier);
    }

    // -- identity without a marker ----------------------------------------------------------------

    /// <summary>
    /// The archetype carries no marker, so it is satisfied by whatever holds the component set. That
    /// is what lets it name entities the core created rather than only ones the SDK did.
    /// </summary>
    [Fact]
    public void An_archetype_over_borrowed_storage_carries_no_marker()
    {
        var thing = Entities.Create<NetworkedThing>();
        var archetype = Raw(thing).Archetype;

        Assert.Equal(2, archetype.ComponentTypes.Count);
        Assert.True(archetype.ComponentTypes.Has<CoreMetadataComponent>());
        Assert.True(archetype.ComponentTypes.Has<CoreScopeComponent>());
    }

    /// An entity assembled by hand, the way a core would, still answers to the archetype.
    [Fact]
    public void An_entity_the_sdk_did_not_create_is_still_recognised()
    {
        var entity = Store.CreateEntity();

        entity.AddComponent(new CoreMetadataComponent(1, 2, "core"));
        entity.AddComponent(new CoreScopeComponent { AreaIdentifier = 4 });

        var handle = EntityHandle.Of(Entities.Create<NetworkedThing>());
        var borrowed = handle.For(entity.RawEntity);

        Assert.True(borrowed.Is<NetworkedThing>());
        Assert.True(borrowed.TryAs<NetworkedThing>(out var thing));
        Assert.Equal(1, thing.NetId);
        Assert.Equal(4, thing.Area);
    }

    [Fact]
    public void A_query_finds_an_entity_the_sdk_did_not_create()
    {
        var entity = Store.CreateEntity();

        entity.AddComponent(new CoreMetadataComponent(5, 6, "core"));
        entity.AddComponent(new CoreScopeComponent { AreaIdentifier = 8 });

        var seen = new List<int>();

        foreach (var thing in Entities.Query<NetworkedThing>())
            seen.Add(thing.NetId);

        Assert.Equal([5], seen);
    }

    // -- mixing owned and borrowed storage --------------------------------------------------------

    /// <summary>An archetype keeps its own generated component, and still drops the marker.</summary>
    [Fact]
    public void An_archetype_may_mix_its_own_component_with_a_borrowed_one()
    {
        var tracked = Entities.Create<Tracked>();

        tracked.Ticks = 11;
        tracked.Owner = 12;

        Assert.Equal(11, tracked.Ticks);
        Assert.Equal(12, tracked.Owner);
        Assert.Equal(2, Store.GetEntityByRawEntity(EntityHandle.Of(tracked).RawEntity).Archetype.ComponentTypes.Count);
    }

    /// A mixin over borrowed storage is queryable on its own, across every archetype carrying it.
    [Fact]
    public void A_borrowed_mixin_is_queryable_across_archetypes()
    {
        Entities.Create<NetworkedThing>().Owner = 1;
        Entities.Create<Tracked>().Owner = 2;

        var owners = new List<int>();

        foreach (var metadata in Entities.Query<Metadata>())
            owners.Add(metadata.Owner);

        owners.Sort();
        Assert.Equal([1, 2], owners);
    }

    [Fact]
    public void The_component_set_names_the_borrowed_component()
    {
        var set = MetadataAccessors.Components;

        Assert.Equal(1, set.Count);
        Assert.True(ClientComponents.Resolve(set).Has<CoreMetadataComponent>());
    }
}
