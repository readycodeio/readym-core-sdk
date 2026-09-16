using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Queries over several shapes at once, up to the six the composed views go to.
/// </summary>
/// <remarks>
/// Each shape is given a value nothing else has, so a test failing on a value rather than a count
/// means the slots were split in the wrong place, which is the thing most likely to go wrong as
/// arities are added.
/// </remarks>
public class ServerMultiShapeQueryTests : ServerSdkTest
{
    private Adventurer Spawned()
    {
        var adventurer = Spawn<Adventurer>();

        adventurer.X = 1f;
        adventurer.Hp = 2;
        adventurer.Label = "three";
        adventurer.Pace = 4f;
        adventurer.Gold = 5;
        adventurer.Spirit = 6f;

        return adventurer;
    }

    [Fact]
    public void Three_shapes()
    {
        Spawned();
        Relay.ResetCounters();

        var visited = 0;

        foreach (var (position, vitals, named) in Entities.Query<Position, Vitals, Named>())
        {
            Assert.Equal(1f, position.X);
            Assert.Equal(2, vitals.Hp);
            Assert.Equal("three", named.Label);
            visited++;
        }

        Assert.Equal(1, visited);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    [Fact]
    public void Four_shapes()
    {
        Spawned();
        Relay.ResetCounters();

        var visited = 0;

        foreach (var (position, vitals, named, speed) in Entities.Query<Position, Vitals, Named, Speed>())
        {
            Assert.Equal(1f, position.X);
            Assert.Equal(2, vitals.Hp);
            Assert.Equal("three", named.Label);
            Assert.Equal(4f, speed.Pace);
            visited++;
        }

        Assert.Equal(1, visited);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    [Fact]
    public void Five_shapes()
    {
        Spawned();
        Relay.ResetCounters();

        var visited = 0;

        foreach (var (position, vitals, named, speed, wealth)
                 in Entities.Query<Position, Vitals, Named, Speed, Wealth>())
        {
            Assert.Equal(1f, position.X);
            Assert.Equal(2, vitals.Hp);
            Assert.Equal("three", named.Label);
            Assert.Equal(4f, speed.Pace);
            Assert.Equal(5, wealth.Gold);
            visited++;
        }

        Assert.Equal(1, visited);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    [Fact]
    public void Six_shapes()
    {
        Spawned();
        Relay.ResetCounters();

        var visited = 0;

        foreach (var (position, vitals, named, speed, wealth, mood)
                 in Entities.Query<Position, Vitals, Named, Speed, Wealth, Mood>())
        {
            Assert.Equal(1f, position.X);
            Assert.Equal(2, vitals.Hp);
            Assert.Equal("three", named.Label);
            Assert.Equal(4f, speed.Pace);
            Assert.Equal(5, wealth.Gold);
            Assert.Equal(6f, mood.Spirit);
            visited++;
        }

        Assert.Equal(1, visited);
        Assert.Equal(1, Relay.QueryCalls);
        Assert.Equal(0, Relay.SlotCalls);
    }

    /// Order is the caller's, so the same shapes asked for backwards still land in the right names.
    [Fact]
    public void The_order_asked_for_is_the_order_handed_back()
    {
        Spawned();

        foreach (var (mood, wealth, speed, named, vitals, position)
                 in Entities.Query<Mood, Wealth, Speed, Named, Vitals, Position>())
        {
            Assert.Equal(6f, mood.Spirit);
            Assert.Equal(5, wealth.Gold);
            Assert.Equal(4f, speed.Pace);
            Assert.Equal("three", named.Label);
            Assert.Equal(2, vitals.Hp);
            Assert.Equal(1f, position.X);
        }
    }

    [Fact]
    public void A_write_through_any_of_six_reaches_the_relay()
    {
        var adventurer = Spawned();

        foreach (var (position, vitals, named, speed, wealth, mood)
                 in Entities.Query<Position, Vitals, Named, Speed, Wealth, Mood>())
        {
            position.X = 10f;
            vitals.Hp = 20;
            named.Label = "thirty";
            speed.Pace = 40f;
            wealth.Gold = 50;
            mood.Spirit = 60f;
        }

        var entity = IdentityOf(adventurer);

        Assert.Equal(10f, Relay.Get<PositionComponent>(entity).x);
        Assert.Equal(20, Relay.Get<VitalsComponent>(entity).hp);
        Assert.Equal("thirty", Relay.Get<NamedComponent>(entity).label);
        Assert.Equal(40f, Relay.Get<SpeedComponent>(entity).pace);
        Assert.Equal(50, Relay.Get<WealthComponent>(entity).gold);
        Assert.Equal(60f, Relay.Get<MoodComponent>(entity).spirit);
    }

    /// An entity missing any one of the shapes is not visited.
    [Fact]
    public void Every_shape_asked_for_has_to_be_present()
    {
        Spawn<Npc>();
        Spawn<Boulder>();

        var count = 0;

        foreach (var (_, _, _, _, _, _) in Entities.Query<Position, Vitals, Named, Speed, Wealth, Mood>())
            count++;

        Assert.Equal(0, count);
    }
}
