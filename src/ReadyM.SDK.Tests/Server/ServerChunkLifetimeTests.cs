using ReadyM.SDK.Entity;
using ReadyM.SDK.Exceptions;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// What a chunk descriptor is still worth once the query call that produced it has returned.
/// </summary>
/// <remarks>
/// The loop pulls and the relay pushes, so the SDK keeps what a chunk callback handed it and walks
/// it afterwards. Everything here is about that gap. The relay fills one scratch buffer per
/// archetype and pins it only for the call, so anything read from it later is either the next
/// archetype's entities or memory nothing is holding still.
/// </remarks>
public class ServerChunkLifetimeTests : ServerSdkTest
{
    /// <summary>
    /// Position is on both Npc and Boulder, so this query spans two archetypes and two callbacks.
    /// The identity path buffers by copying and is the control.
    /// </summary>
    [Fact]
    public void Handles_from_a_query_spanning_two_archetypes_name_the_right_entities()
    {
        var expected = new List<int>();

        for (var i = 0; i < 3; i++)
            expected.Add(IdentityOf(Spawn<Npc>()).Id);

        for (var i = 0; i < 3; i++)
            expected.Add(IdentityOf(Spawn<Boulder>()).Id);

        var seen = new List<int>();

        foreach (var position in Entities.Query<Position>())
            seen.Add(position.Handle.Id);

        Assert.Equal(expected.OrderBy(id => id), seen.OrderBy(id => id));
    }

    /// <summary>Each entity visited once, which aliased chunks would break by repeating a run.</summary>
    [Fact]
    public void A_query_spanning_two_archetypes_hands_back_no_duplicate_handles()
    {
        for (var i = 0; i < 3; i++)
        {
            Spawn<Npc>();
            Spawn<Boulder>();
        }

        var seen = new List<int>();

        foreach (var position in Entities.Query<Position>())
            seen.Add(position.Handle.Id);

        Assert.Equal(6, seen.Count);
        Assert.Equal(6, seen.Distinct().Count());
    }

    /// A handle taken in one chunk still names its own entity after later chunks are bound.
    [Fact]
    public void A_handle_kept_past_its_chunk_still_names_its_own_entity()
    {
        var first = IdentityOf(Spawn<Npc>());

        for (var i = 0; i < 3; i++)
            Spawn<Boulder>();

        var kept = default(EntityHandle);
        var taken = false;

        foreach (var position in Entities.Query<Position>())
        {
            if (taken)
                continue;

            kept = position.Handle;
            taken = true;
        }

        Assert.Equal(first.Id, kept.Id);
    }

    /// The two paths walk the same query, so the entities they name have to agree.
    [Fact]
    public void The_chunk_path_and_the_identity_path_name_the_same_entities()
    {
        for (var i = 0; i < 4; i++)
        {
            Spawn<Npc>();
            Spawn<Boulder>();
        }

        var buffered = new List<int>();
        var entities = Entities.Query<Position>().Identities();

        while (entities.MoveNext())
            buffered.Add(EntityHandle.Of(entities.Current).Id);

        entities.Dispose();

        var scanned = new List<int>();

        foreach (var position in Entities.Query<Position>())
            scanned.Add(position.Handle.Id);

        Assert.Equal(buffered.OrderBy(id => id), scanned.OrderBy(id => id));
    }

    // -- structural change during a loop -----------------------------------------------------------

    /// <summary>
    /// Why creating is refused, stated as a test. A chunk names a component array once, which is what
    /// makes the fast path fast; growing an archetype allocates a new array and copies, so a chunk
    /// bound beforehand names storage nothing reads any more. Reads went stale and writes vanished,
    /// silently, until the refusal closed it.
    /// </summary>
    [Fact]
    public void Nothing_can_grow_an_archetype_while_a_chunk_loop_is_walking_it()
    {
        for (var i = 0; i < 5; i++)
            Spawn<Npc>();

        // Written the way READYM002 forbids, deliberately: the analyzer stops this at the call
        // site, and this checks the run time refuses it too, which is what covers a call the
        // analyzer cannot see through.
#pragma warning disable READYM002
        Assert.Throws<StructuralChangeInQueryException>(() =>
        {
            foreach (var npc in Entities.Query<Npc>())
            {
                Entities.Create<Npc>();
                npc.Faction = 100;
            }
        });
#pragma warning restore READYM002
    }

    /// <summary>A delete is the one structural change a loop may ask for, because it is held.</summary>
    [Fact]
    public void Deleting_is_allowed_where_creating_is_not()
    {
        for (var i = 0; i < 5; i++)
            Spawn<Npc>();

        foreach (var npc in Entities.Query<Npc>())
            Entities.Delete(npc);

        Assert.Equal(0, CountIdentities(Entities.Query<Npc>()));
    }

    /// <summary>
    /// The case READYM002 cannot see: the create is behind a call, so only the run time catches it.
    /// This is why the refusal exists in both places rather than only in the analyzer.
    /// </summary>
    [Fact]
    public void A_create_reached_through_a_helper_is_still_refused()
    {
        Spawn<Npc>();

        Assert.Throws<StructuralChangeInQueryException>(() =>
        {
            foreach (var _ in Entities.Query<Npc>())
                SpawnAnother();
        });
    }

    private void SpawnAnother() => Entities.Create<Npc>();
}
