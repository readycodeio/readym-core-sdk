using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// The client walking chunks rather than identities. Same store, same Friflo, no interop: the win
/// here is skipping the per-access entity resolution, not a boundary crossing.
/// </summary>
public class ChunkQueryTests : ClientSdkTest
{
    /// <summary>
    /// A compile-time assertion as much as a test: the loop variable binds to the chunk view, which
    /// it could not if the query were still handing out identities.
    /// </summary>
    [Fact]
    public void A_query_over_a_named_shape_yields_a_chunk_view()
    {
        SpawnMonster(level: 9);

        foreach (var monster in Entities.Query<Monster>())
            Assert.Equal(9, LevelOf(monster));

        static int LevelOf(MonsterView view) => view.Level;
    }

    [Fact]
    public void A_chunk_loop_reads_what_was_written()
    {
        SpawnMonster(level: 3, hp: 12f, x: 4f, y: 5f);

        var visited = 0;

        foreach (var monster in Entities.Query<Monster>())
        {
            Assert.Equal(3, monster.Level);
            Assert.Equal(12f, monster.Hp);
            Assert.Equal(4f, monster.X);
            Assert.Equal(5f, monster.Y);
            visited++;
        }

        Assert.Equal(1, visited);
    }

    [Fact]
    public void A_chunk_loop_writes_back()
    {
        var monster = SpawnMonster();

        foreach (var found in Entities.Query<Monster>())
        {
            found.Hp = 42f;
            found.Level = 7;
        }

        Assert.Equal(42f, monster.Hp);
        Assert.Equal(7, monster.Level);
    }

    /// <summary>The two paths are the same query, so they have to agree.</summary>
    [Fact]
    public void The_chunk_path_and_the_identity_path_agree()
    {
        for (var i = 0; i < 12; i++)
            SpawnMonster(level: i, hp: i * 2f, x: i * 3f);

        var identities = new List<string>();
        var chunks = new List<string>();

        var walk = Entities.Query<Monster>().Identities();

        while (walk.MoveNext())
        {
            var monster = walk.Current;
            identities.Add($"{monster.Level}/{monster.Hp}/{monster.X}");
        }

        walk.Dispose();

        foreach (var monster in Entities.Query<Monster>())
            chunks.Add($"{monster.Level}/{monster.Hp}/{monster.X}");

        Assert.Equal(identities, chunks);
        Assert.Equal(12, chunks.Count);
    }

    /// <summary>A chunk spans one archetype, so several archetypes means several chunks.</summary>
    [Fact]
    public void A_query_spanning_several_archetypes_visits_them_all()
    {
        SpawnMonster(x: 1f);
        SpawnChest(x: 2f);
        SpawnProp(x: 4f);

        var total = 0f;

        foreach (var placement in Entities.Query<Placement>())
            total += placement.X;

        Assert.Equal(7f, total);
    }

    /// The handle is how work that cannot happen inside the loop gets out of it.
    [Fact]
    public void A_view_hands_back_a_handle_for_the_entity_it_is_on()
    {
        var monster = SpawnMonster();
        var expected = EntityHandle.Of(monster).Id;

        foreach (var found in Entities.Query<Monster>())
        {
            Assert.Equal(expected, found.Handle.Id);
            Assert.True(found.Handle.IsAlive());
        }
    }
}
