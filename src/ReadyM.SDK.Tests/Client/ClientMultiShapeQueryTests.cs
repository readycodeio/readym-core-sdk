using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// Every width a client query goes to, reading and writing through each shape it names.
public class ClientMultiShapeQueryTests : ClientSdkTest
{
    private Wanderer Spawned()
    {
        var wanderer = Entities.Create<Wanderer>();

        wanderer.Hp = 1f;
        wanderer.X = 2f;
        wanderer.Pace = 3f;
        wanderer.Gold = 4;
        wanderer.Spirit = 5f;
        wanderer.Text = "six";

        return wanderer;
    }

    private void AssertWritten(Wanderer wanderer, int shapes)
    {
        var expected = new object[] { 11f, 22f, 33f, 44, 55f, "sixty" };
        var actual = new object[] { wanderer.Hp, wanderer.X, wanderer.Pace, wanderer.Gold, wanderer.Spirit, wanderer.Text };

        for (var i = 0; i < shapes; i++)
            Assert.Equal(expected[i], actual[i]);
    }

    [Fact]
    public void A_query_over_one_shapes_reads_every_one()
    {
        Spawned();

        var visited = 0;

        foreach (var health in Entities.Query<Health>())
        {
            Assert.Equal(1f, health.Hp);
            visited++;
        }

        Assert.Equal(1, visited);
    }

    [Fact]
    public void A_write_through_one_shapes_lands_on_the_entity()
    {
        var wanderer = Spawned();

        foreach (var health in Entities.Query<Health>())
        {
            health.Hp = 11f;
        }

        AssertWritten(wanderer, 1);
    }

    /// The path a netstandard2.0 client always takes, and the one this arity was written for.
    [Fact]
    public void The_identity_path_over_one_shapes_reads_and_writes()
    {
        var wanderer = Spawned();

        var entities = Entities.Query<Health>().Identities();
        var visited = 0;

        while (entities.MoveNext())
        {
            var health = entities.Current;

            Assert.Equal(1f, health.Hp);
            health.Hp = 11f;
            visited++;
        }

        entities.Dispose();

        Assert.Equal(1, visited);
        AssertWritten(wanderer, 1);
    }

    [Fact]
    public void A_query_over_two_shapes_reads_every_one()
    {
        Spawned();

        var visited = 0;

        foreach (var (health, placement) in Entities.Query<Health, Placement>())
        {
            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            visited++;
        }

        Assert.Equal(1, visited);
    }

    [Fact]
    public void A_write_through_two_shapes_lands_on_the_entity()
    {
        var wanderer = Spawned();

        foreach (var (health, placement) in Entities.Query<Health, Placement>())
        {
            health.Hp = 11f;
            placement.X = 22f;
        }

        AssertWritten(wanderer, 2);
    }

    /// The path a netstandard2.0 client always takes, and the one this arity was written for.
    [Fact]
    public void The_identity_path_over_two_shapes_reads_and_writes()
    {
        var wanderer = Spawned();

        var entities = Entities.Query<Health, Placement>().Identities();
        var visited = 0;

        while (entities.MoveNext())
        {
            var (health, placement) = entities.Current;

            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            health.Hp = 11f;
            placement.X = 22f;
            visited++;
        }

        entities.Dispose();

        Assert.Equal(1, visited);
        AssertWritten(wanderer, 2);
    }

    [Fact]
    public void A_query_over_three_shapes_reads_every_one()
    {
        Spawned();

        var visited = 0;

        foreach (var (health, placement, stride) in Entities.Query<Health, Placement, Stride>())
        {
            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            Assert.Equal(3f, stride.Pace);
            visited++;
        }

        Assert.Equal(1, visited);
    }

    [Fact]
    public void A_write_through_three_shapes_lands_on_the_entity()
    {
        var wanderer = Spawned();

        foreach (var (health, placement, stride) in Entities.Query<Health, Placement, Stride>())
        {
            health.Hp = 11f;
            placement.X = 22f;
            stride.Pace = 33f;
        }

        AssertWritten(wanderer, 3);
    }

    /// The path a netstandard2.0 client always takes, and the one this arity was written for.
    [Fact]
    public void The_identity_path_over_three_shapes_reads_and_writes()
    {
        var wanderer = Spawned();

        var entities = Entities.Query<Health, Placement, Stride>().Identities();
        var visited = 0;

        while (entities.MoveNext())
        {
            var (health, placement, stride) = entities.Current;

            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            Assert.Equal(3f, stride.Pace);
            health.Hp = 11f;
            placement.X = 22f;
            stride.Pace = 33f;
            visited++;
        }

        entities.Dispose();

        Assert.Equal(1, visited);
        AssertWritten(wanderer, 3);
    }

    [Fact]
    public void A_query_over_four_shapes_reads_every_one()
    {
        Spawned();

        var visited = 0;

        foreach (var (health, placement, stride, purse) in Entities.Query<Health, Placement, Stride, Purse>())
        {
            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            Assert.Equal(3f, stride.Pace);
            Assert.Equal(4, purse.Gold);
            visited++;
        }

        Assert.Equal(1, visited);
    }

    [Fact]
    public void A_write_through_four_shapes_lands_on_the_entity()
    {
        var wanderer = Spawned();

        foreach (var (health, placement, stride, purse) in Entities.Query<Health, Placement, Stride, Purse>())
        {
            health.Hp = 11f;
            placement.X = 22f;
            stride.Pace = 33f;
            purse.Gold = 44;
        }

        AssertWritten(wanderer, 4);
    }

    /// The path a netstandard2.0 client always takes, and the one this arity was written for.
    [Fact]
    public void The_identity_path_over_four_shapes_reads_and_writes()
    {
        var wanderer = Spawned();

        var entities = Entities.Query<Health, Placement, Stride, Purse>().Identities();
        var visited = 0;

        while (entities.MoveNext())
        {
            var (health, placement, stride, purse) = entities.Current;

            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            Assert.Equal(3f, stride.Pace);
            Assert.Equal(4, purse.Gold);
            health.Hp = 11f;
            placement.X = 22f;
            stride.Pace = 33f;
            purse.Gold = 44;
            visited++;
        }

        entities.Dispose();

        Assert.Equal(1, visited);
        AssertWritten(wanderer, 4);
    }

    [Fact]
    public void A_query_over_five_shapes_reads_every_one()
    {
        Spawned();

        var visited = 0;

        foreach (var (health, placement, stride, purse, mood) in Entities.Query<Health, Placement, Stride, Purse, Mood>())
        {
            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            Assert.Equal(3f, stride.Pace);
            Assert.Equal(4, purse.Gold);
            Assert.Equal(5f, mood.Spirit);
            visited++;
        }

        Assert.Equal(1, visited);
    }

    [Fact]
    public void A_write_through_five_shapes_lands_on_the_entity()
    {
        var wanderer = Spawned();

        foreach (var (health, placement, stride, purse, mood) in Entities.Query<Health, Placement, Stride, Purse, Mood>())
        {
            health.Hp = 11f;
            placement.X = 22f;
            stride.Pace = 33f;
            purse.Gold = 44;
            mood.Spirit = 55f;
        }

        AssertWritten(wanderer, 5);
    }

    /// The path a netstandard2.0 client always takes, and the one this arity was written for.
    [Fact]
    public void The_identity_path_over_five_shapes_reads_and_writes()
    {
        var wanderer = Spawned();

        var entities = Entities.Query<Health, Placement, Stride, Purse, Mood>().Identities();
        var visited = 0;

        while (entities.MoveNext())
        {
            var (health, placement, stride, purse, mood) = entities.Current;

            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            Assert.Equal(3f, stride.Pace);
            Assert.Equal(4, purse.Gold);
            Assert.Equal(5f, mood.Spirit);
            health.Hp = 11f;
            placement.X = 22f;
            stride.Pace = 33f;
            purse.Gold = 44;
            mood.Spirit = 55f;
            visited++;
        }

        entities.Dispose();

        Assert.Equal(1, visited);
        AssertWritten(wanderer, 5);
    }

    [Fact]
    public void A_query_over_six_shapes_reads_every_one()
    {
        Spawned();

        var visited = 0;

        foreach (var (health, placement, stride, purse, mood, label) in Entities.Query<Health, Placement, Stride, Purse, Mood, Label>())
        {
            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            Assert.Equal(3f, stride.Pace);
            Assert.Equal(4, purse.Gold);
            Assert.Equal(5f, mood.Spirit);
            Assert.Equal("six", label.Text);
            visited++;
        }

        Assert.Equal(1, visited);
    }

    [Fact]
    public void A_write_through_six_shapes_lands_on_the_entity()
    {
        var wanderer = Spawned();

        foreach (var (health, placement, stride, purse, mood, label) in Entities.Query<Health, Placement, Stride, Purse, Mood, Label>())
        {
            health.Hp = 11f;
            placement.X = 22f;
            stride.Pace = 33f;
            purse.Gold = 44;
            mood.Spirit = 55f;
            label.Text = "sixty";
        }

        AssertWritten(wanderer, 6);
    }

    /// The path a netstandard2.0 client always takes, and the one this arity was written for.
    [Fact]
    public void The_identity_path_over_six_shapes_reads_and_writes()
    {
        var wanderer = Spawned();

        var entities = Entities.Query<Health, Placement, Stride, Purse, Mood, Label>().Identities();
        var visited = 0;

        while (entities.MoveNext())
        {
            var (health, placement, stride, purse, mood, label) = entities.Current;

            Assert.Equal(1f, health.Hp);
            Assert.Equal(2f, placement.X);
            Assert.Equal(3f, stride.Pace);
            Assert.Equal(4, purse.Gold);
            Assert.Equal(5f, mood.Spirit);
            Assert.Equal("six", label.Text);
            health.Hp = 11f;
            placement.X = 22f;
            stride.Pace = 33f;
            purse.Gold = 44;
            mood.Spirit = 55f;
            label.Text = "sixty";
            visited++;
        }

        entities.Dispose();

        Assert.Equal(1, visited);
        AssertWritten(wanderer, 6);
    }

    /// Every shape asked for has to be present, so a narrower entity is skipped.
    [Fact]
    public void An_entity_missing_one_of_the_shapes_is_not_visited()
    {
        Entities.Create<Chest>();

        var count = 0;

        foreach (var (_, _, _, _, _, _) in Entities.Query<Health, Placement, Stride, Purse, Mood, Label>())
            count++;

        Assert.Equal(0, count);
    }

    /// Order is the caller's, so the same shapes asked for backwards land in the right names.
    [Fact]
    public void The_order_asked_for_is_the_order_handed_back()
    {
        Spawned();

        foreach (var (label, mood, purse, stride, placement, health)
                 in Entities.Query<Label, Mood, Purse, Stride, Placement, Health>())
        {
            Assert.Equal("six", label.Text);
            Assert.Equal(5f, mood.Spirit);
            Assert.Equal(4, purse.Gold);
            Assert.Equal(3f, stride.Pace);
            Assert.Equal(2f, placement.X);
            Assert.Equal(1f, health.Hp);
        }
    }
}
