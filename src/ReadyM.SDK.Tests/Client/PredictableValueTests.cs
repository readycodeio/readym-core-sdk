using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Events;
using ReadyM.SDK.Tests.Client.Fixtures;
using Xunit;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// A [Predictable] value through the accessors the generator actually emitted, rather than through a stand-in
/// for them. What a mod sees is one property: it reads the guess while one stands and the server's value once
/// it does not, and never learns which it got.
/// </summary>
/// <remarks>
/// The guess is written into the component itself, because the component is what the game is told about. The
/// tests below read the component as well as the property for that reason: a value only C# could see would
/// leave the game showing the server's value the whole time.
/// </remarks>
public class PredictableValueTests : ClientSdkTest
{
    private const RelayMessageCode Code = (RelayMessageCode)ushort.MaxValue;

    /// <summary>Which bit of the delta mask each of the fixture's fields is.</summary>
    private const byte ActiveBit = 1 << 0;

    private const byte HourBit = 1 << 1;

    private PredictionDeltaHooks<LockdownComponent> Hooks => DeltaHooks.For<LockdownComponent>();

    private Settlement Town()
    {
        var town = Entities.Create<Settlement>();

        // Settled, the way a replicated entity is once the server's snapshot of it has been read
        Component(town).ClearDirty();
        Component(town).ClearApiFlag();

        return town;
    }

    private static ref LockdownComponent Component(Settlement town)
        => ref EntityHandle.Of(town).GetComponent<LockdownComponent>();

    /// <summary>The fields the next delta this client sends would carry, as the mask at its head.</summary>
    private static byte Outgoing(Settlement town)
    {
        var writer = new NetDataWriter();

        Component(town).WriteDelta(writer);

        return writer.Data[0];
    }

    /// <summary>
    /// The server's value arriving as the delta path delivers it: the hooks either side of the read, around a
    /// delta carrying only what changed on the server's copy.
    /// </summary>
    private void Arrives(Settlement town, bool? active = null, int? hour = null)
    {
        // Moved off the value first, so the write that follows is a change and the delta carries it
        var server = default(LockdownComponent);

        if (active is { } changedActive)
            server.Active = !changedActive;

        if (hour is { } changedHour)
            server.Hour = changedHour + 1;

        server.ClearDirty();

        if (active is { } carriedActive)
            server.Active = carriedActive;

        if (hour is { } carriedHour)
            server.Hour = carriedHour;

        var writer = new NetDataWriter();
        server.WriteDelta(writer);

        ref var component = ref Component(town);
        var id = EntityHandle.Of(town).Id;

        Hooks.BeforeDeltaRead?.Invoke(id, ref component);
        component.ReadDeltaTracking(new NetDataReader(writer.CopyData()), id);
        Hooks.AfterDeltaRead?.Invoke(id, ref component);
    }

    [Fact]
    public void A_value_nobody_has_guessed_at_reads_as_the_server_left_it()
    {
        var town = Town();

        town.LockdownActive = true;

        Assert.True(town.LockdownActive);
    }

    [Fact]
    public void A_guess_is_what_the_value_reads_as_while_it_stands()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));

        Assert.True(town.LockdownActive);
    }

    /// <summary>
    /// The game reads the component, not the property, so a guess held anywhere else is a guess nobody sees.
    /// It goes in as a change from the API, which is the mark that has the game take a value from the ECS.
    /// </summary>
    [Fact]
    public void A_guess_is_written_into_the_component_for_the_game_to_show()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));

        Assert.True(Component(town).Active);
        Assert.True(Component(town).ChangedFromApi);
    }

    /// <summary>
    /// A guess is this client's alone. Sent, it would reach the server as the value itself, which is the one
    /// thing a guess at a value the server owns must never do.
    /// </summary>
    [Fact]
    public void A_guess_is_never_sent()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));

        Assert.False(Component(town).IsDirty);
        Assert.Equal(0, Outgoing(town));
    }

    /// <summary>
    /// The game copying the guessed value back into the component, or anything else marking it changed while
    /// the guess stands, still does not send it: the outgoing delta leaves the guessed field out, and only it.
    /// </summary>
    [Fact]
    public void A_guessed_field_is_held_out_of_what_is_sent_while_the_guess_stands()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));

        Component(town).Active = false;
        Component(town).Hour = 22;
        Hooks.BeforeDeltaSend?.Invoke();

        Assert.Equal(HourBit, Outgoing(town));
    }

    [Fact]
    public void A_refused_guess_puts_the_servers_value_back_for_the_game_to_show()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));
        Component(town).ClearApiFlag();
        PredictedRaises.RollBackRefused(Code);

        Assert.False(town.LockdownActive);
        Assert.False(Component(town).Active);
        Assert.True(Component(town).ChangedFromApi);
        Assert.Equal(0, Outgoing(town));
    }

    [Fact]
    public void A_guess_nothing_answered_is_taken_back_when_it_runs_out()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));
        PredictedRaises.RollBackExpired(PredictedRaiseRegistry.Timeout);

        Assert.False(town.LockdownActive);
    }

    /// <summary>One entity's guess is not another's, which is what keying the guess by entity is for.</summary>
    [Fact]
    public void A_guess_about_one_entity_leaves_the_others_alone()
    {
        var guessed = Town();
        var other = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => guessed.PredictLockdownActive(true));

        Assert.True(guessed.LockdownActive);
        Assert.False(other.LockdownActive);
    }

    /// <summary>
    /// Only the marked value is guessed at. Writing its neighbour inside the same callback goes through to the
    /// component as it always does, so it is still there once the guess has been taken back.
    /// </summary>
    [Fact]
    public void A_value_that_is_not_marked_is_written_rather_than_guessed()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() =>
        {
            town.PredictLockdownActive(true);
            town.LockdownHour = 22;
        });

        PredictedRaises.RollBackRefused(Code);

        Assert.False(town.LockdownActive);
        Assert.Equal(22, town.LockdownHour);
    }

    /// <summary>
    /// The shape's own property and the archetype's projection of it read one value. Two stores, or a guess
    /// held on the projection alone, would leave the same value reading two ways on one entity.
    /// </summary>
    [Fact]
    public void The_shape_and_the_archetype_read_one_guess()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));

        Assert.True(town.As<Lockdown>().Active);
        Assert.True(town.LockdownActive);
    }

    /// <summary>
    /// A write is a write wherever it is. Guessing is its own call, so nothing has to tell the two apart by
    /// where control came from, and nothing is quietly held back.
    /// </summary>
    [Fact]
    public void A_write_is_a_write_and_reaches_the_value()
    {
        var town = Town();

        town.LockdownActive = true;
        PredictedRaises.RollBackExpired(PredictedRaiseRegistry.Timeout * 10);

        Assert.True(town.LockdownActive);
        Assert.Equal(ActiveBit, Outgoing(town));
    }

    /// <summary>
    /// The server's own value arriving is what answers an accepted guess, long before the deadline would.
    /// Once it has, the deadline coming round finds nothing to take back and leaves the server's value alone.
    /// </summary>
    [Fact]
    public void The_value_arriving_from_the_server_answers_the_guess()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));

        Arrives(town, active: true);
        PredictedRaises.RollBackExpired(PredictedRaiseRegistry.Timeout);

        Assert.True(town.LockdownActive);
        Assert.True(Component(town).Active);
    }

    /// <summary>
    /// The server answering with something other than the guess: what arrived is what reads, rather than the
    /// guess standing in front of it until the deadline.
    /// </summary>
    [Fact]
    public void A_different_answer_from_the_server_replaces_the_guess_at_once()
    {
        var town = Town();
        town.LockdownActive = true;
        Component(town).ClearDirty();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(false));
        Assert.False(town.LockdownActive);

        Arrives(town, active: false);
        PredictedRaises.RollBackExpired(PredictedRaiseRegistry.Timeout);

        Assert.False(town.LockdownActive);
        Assert.False(Component(town).Active);
    }

    /// <summary>
    /// A delta carries only the fields that changed. One that moved the field beside the guess says nothing
    /// about the guess, which keeps standing rather than flickering back to the old value until the real
    /// answer arrives.
    /// </summary>
    [Fact]
    public void A_delta_that_does_not_carry_the_guessed_value_leaves_the_guess_standing()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));

        Arrives(town, hour: 7);

        Assert.True(town.LockdownActive);
        Assert.Equal(7, town.LockdownHour);

        PredictedRaises.RollBackExpired(PredictedRaiseRegistry.Timeout);

        Assert.False(town.LockdownActive);
        Assert.Equal(7, town.LockdownHour);
    }

    /// <summary>A delta for one entity says nothing about a guess standing on another.</summary>
    [Fact]
    public void A_value_arriving_for_one_entity_leaves_another_entitys_guess_alone()
    {
        var answered = Town();
        var waiting = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() =>
        {
            answered.PredictLockdownActive(true);
            waiting.PredictLockdownActive(true);
        });

        Arrives(answered, active: true);
        PredictedRaises.RollBackExpired(PredictedRaiseRegistry.Timeout);

        Assert.True(answered.LockdownActive);
        Assert.False(waiting.LockdownActive);
    }

    /// <summary>The delta path asks about guesses only while one stands. With none it pays a null check.</summary>
    [Fact]
    public void Nothing_listens_to_the_delta_path_once_no_guess_stands()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));
        Assert.NotNull(Hooks.BeforeDeltaRead);

        PredictedRaises.RollBackRefused(Code);

        Assert.Null(Hooks.BeforeDeltaRead);
        Assert.Null(Hooks.AfterDeltaRead);
        Assert.Null(Hooks.BeforeDeltaSend);
    }

    /// <summary>
    /// A guess with no raise waiting on an answer would stand for the rest of the session, so it says so
    /// rather than holding a value nothing will take back. The member is named, because the line that made
    /// the guess is not always the line that reads it.
    /// </summary>
    [Fact]
    public void Guessing_with_nothing_waiting_on_an_answer_says_so()
    {
        var town = Town();

        var thrown = Assert.Throws<InvalidOperationException>(() => town.PredictLockdownActive(true));

        Assert.Contains("Lockdown.Active", thrown.Message);
        Assert.False(town.LockdownActive);
    }
}
