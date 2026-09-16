using ReadyM.SDK.Entity;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// The revision half of an identity, which is the whole reason the v1 ABI carries a RawEntity rather
/// than an id. The relay recycles ids, so an id alone cannot say whether a handle is still good.
/// </summary>
public class ServerIdentityTests : ServerSdkTest
{
    [Fact]
    public void A_live_entity_is_valid()
    {
        Assert.True(Spawn<Npc>().IsValid);
    }

    [Fact]
    public void A_deleted_entity_is_not_valid()
    {
        var entity = Spawn<Npc>();
        Relay.Kill(IdentityOf(entity));

        Assert.False(entity.IsValid);
    }

    [Fact]
    public void A_handle_kept_across_a_recycled_id_is_reported_as_gone()
    {
        var entity = Spawn<Npc>();
        var identity = IdentityOf(entity);

        Relay.Kill(identity);
        var reused = Relay.Recycle(identity);

        Assert.Equal(identity.Id, reused.Id);
        Assert.False(entity.IsValid);
        Assert.True(Wrap<Npc>(reused).IsValid);
    }

    [Fact]
    public void Reading_through_a_stale_handle_throws_rather_than_returning_another_entity()
    {
        var entity = Spawn<Npc>();
        var identity = IdentityOf(entity);

        Relay.Kill(identity);
        var reused = Relay.Recycle(identity);
        Relay.Set(reused, new PositionComponent { x = 42f });

        Assert.Throws<ComponentNotFoundException>(() => { _ = entity.X; });
        Assert.Equal(42f, Wrap<Npc>(reused).X);
    }

    [Fact]
    public void An_archetype_reports_whether_the_entity_carries_another_ones_components()
    {
        var npc = Spawn<Npc>();
        var boulder = Spawn<Boulder>();

        Assert.True(npc.Is<Npc>());
        Assert.True(boulder.Is<Boulder>());
        Assert.False(boulder.Is<Npc>());
    }

    [Fact]
    public void An_entity_can_be_cast_to_an_archetype_whose_components_it_carries()
    {
        var npc = Spawn<Npc>();

        Assert.True(EntityHandle.Of(npc).TryAs<Placed>(out var placed));
        Assert.Equal(IdentityOf(npc).Id, EntityHandle.Of(placed).Id);
    }
}
