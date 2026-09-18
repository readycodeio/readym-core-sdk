using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Tests.Client.Fixtures;
using FrifloEntity = Friflo.Engine.ECS.Entity;

namespace ReadyM.SDK.Tests.Client;

/// Collections of an explicit component, reached through the methods the component already offers.
public class ExplicitCollectionTests : ClientSdkTest
{
    private FrifloEntity Raw<T>(T shape) where T : struct, IArchetypeQueryable
        => Store.GetEntityByRawEntity(EntityHandle.Of(shape).RawEntity);

    [Fact]
    public void A_collection_is_reached_through_the_shape()
    {
        var cutscene = Entities.Create<Cutscene>();

        cutscene.AddStarted(4);
        cutscene.AddStarted(9);

        Assert.Equal(2, cutscene.StartedCount);
        Assert.Equal(4, cutscene.GetStarted(0));
        Assert.Equal(9, cutscene.GetStarted(1));
        Assert.True(cutscene.ContainsStarted(9));
    }

    /// <summary>
    /// What forwarding buys over exposing the field: the component's own method runs, so whatever it
    /// does alongside the write still happens.
    /// </summary>
    [Fact]
    public void A_write_goes_through_the_component_so_it_stays_tracked()
    {
        var cutscene = Entities.Create<Cutscene>();

        cutscene.AddStarted(1);
        cutscene.ClearStarted();

        Assert.Equal(2, Raw(cutscene).GetComponent<SequencesComponent>().Changes);
    }

    [Fact]
    public void A_collection_is_reached_the_same_way_from_a_chunk()
    {
        for (var i = 0; i < 3; i++)
            Entities.Create<Cutscene>().AddStarted(i + 1);

        var total = 0;

        foreach (var cutscene in Entities.Query<Cutscene>())
        {
            cutscene.AddStarted(10);
            total += cutscene.StartedCount;
        }

        Assert.Equal(6, total);

        foreach (var cutscene in Entities.Query<Cutscene>())
            Assert.True(cutscene.ContainsStarted(10));
    }

    [Fact]
    public void The_mixin_itself_carries_the_collection()
    {
        Entities.Create<Cutscene>();

        foreach (var sequences in Entities.Query<Sequences>())
            sequences.AddStarted(7);

        foreach (var sequences in Entities.Query<Sequences>())
            Assert.True(sequences.ContainsStarted(7));
    }

    /// Replication plumbing belongs to the component, so a shape must not gain it.
    [Theory]
    [InlineData("Started_SetFromApi")]
    [InlineData("StartedNotifyChanged")]
    public void Plumbing_is_not_forwarded(string name)
        => Assert.DoesNotContain(typeof(Sequences).GetMethods(), method => method.Name == name);

    /// <summary>
    /// A native collection hands back a ref struct over the component's storage. Forwarding one has
    /// to say the handle is scoped, or the shape cannot return it at all.
    /// </summary>
    [Fact]
    public void A_member_handing_back_a_ref_struct_is_forwarded()
    {
        var cutscene = Entities.Create<Cutscene>();

        cutscene.AddStarted(2);
        cutscene.AddStarted(5);

        Assert.Equal([2, 5], cutscene.GetStartedSpan().ToArray());

        foreach (var scanned in Entities.Query<Cutscene>())
            Assert.Equal([2, 5], scanned.GetStartedSpan().ToArray());
    }
}
