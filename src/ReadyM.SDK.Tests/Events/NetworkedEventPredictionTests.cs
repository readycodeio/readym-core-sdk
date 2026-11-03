using System.Collections.Generic;
using ReadyM.Api.Multiplayer.ECS;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.SDK.Events;
using Xunit;

namespace ReadyM.SDK.Tests.Events;

/// <summary>
/// A guess a client makes while it waits for the server's answer, and the three things that take it back: the
/// refusal, its own deadline, and nothing else. Clearing runs the undo the caller gave; it never guesses one.
/// </summary>
public class NetworkedEventPredictionTests
{
    private const RelayMessageCode Code = (RelayMessageCode)ushort.MaxValue;

    private const RelayMessageCode Other = RelayMessageCode.MinAnyCustomEvent;

    private readonly PredictedRaiseRegistry _predictions = new(new GuessedValueRegistry(new PredictionDeltaHooks()));

    private readonly List<string> _ran = [];

    [Fact]
    public void A_guess_runs_the_moment_it_is_made()
    {
        _predictions.StartRaise(Code, 0).Predicting(() => _ran.Add("guess"));

        Assert.Equal(["guess"], _ran);
    }

    /// <summary>
    /// The undo is held, not run. A caller that wanted it to run now would have written it as the guess.
    /// </summary>
    [Fact]
    public void The_undo_waits_for_something_to_undo()
    {
        _predictions.StartRaise(Code, 0).Predicting(() => _ran.Add("guess")).Rollback(() => _ran.Add("undo"));

        Assert.Equal(["guess"], _ran);
    }

    [Fact]
    public void A_refusal_takes_the_guess_back()
    {
        _predictions.StartRaise(Code, 0).Predicting(() => _ran.Add("guess")).Rollback(() => _ran.Add("undo"));

        _predictions.RollBackRefused(Code);

        Assert.Equal(["guess", "undo"], _ran);
    }

    /// <summary>A refusal of one event says nothing about a guess made for another.</summary>
    [Fact]
    public void A_refusal_of_another_event_leaves_this_guess_alone()
    {
        _predictions.StartRaise(Code, 0).Predicting(() => _ran.Add("guess")).Rollback(() => _ran.Add("undo"));

        _predictions.RollBackRefused(Other);

        Assert.Equal(["guess"], _ran);
    }

    /// <summary>
    /// Nothing comes back for an event the server keeps to itself when it accepts one, so without a deadline a
    /// guess would stand for the rest of the session.
    /// </summary>
    [Fact]
    public void A_guess_that_was_never_answered_is_taken_back_when_it_runs_out()
    {
        _predictions.StartRaise(Code, 1000).Predicting(() => _ran.Add("guess")).Rollback(() => _ran.Add("undo"));

        _predictions.RollBackExpired(1000 + PredictedRaiseRegistry.Timeout);

        Assert.Equal(["guess", "undo"], _ran);
    }

    [Fact]
    public void A_guess_still_within_its_deadline_stands()
    {
        _predictions.StartRaise(Code, 1000).Predicting(() => _ran.Add("guess")).Rollback(() => _ran.Add("undo"));

        _predictions.RollBackExpired(1000 + PredictedRaiseRegistry.Timeout - 0.001);

        Assert.Equal(["guess"], _ran);
    }

    /// <summary>An undo runs once, or a second signal would undo what the player has since done again.</summary>
    [Fact]
    public void A_guess_is_taken_back_once_however_many_signals_arrive()
    {
        _predictions.StartRaise(Code, 0).Predicting(() => _ran.Add("guess")).Rollback(() => _ran.Add("undo"));

        _predictions.RollBackRefused(Code);
        _predictions.RollBackRefused(Code);
        _predictions.RollBackExpired(PredictedRaiseRegistry.Timeout * 10);

        Assert.Equal(["guess", "undo"], _ran);
    }

    /// <summary>
    /// A guessed value needs no undo: the read falls through to the replicated one as it stands, which is what
    /// [Predictable] generates. Only a guessed action has something to take back.
    /// </summary>
    [Fact]
    public void A_guess_with_no_undo_clears_without_running_anything()
    {
        _predictions.StartRaise(Code, 0).Predicting(() => _ran.Add("guess"));

        _predictions.RollBackRefused(Code);

        Assert.Equal(["guess"], _ran);
    }

    [Fact]
    public void Several_guesses_for_one_event_all_come_back()
    {
        _predictions.StartRaise(Code, 0).Predicting(() => _ran.Add("one")).Rollback(() => _ran.Add("undo one"));
        _predictions.StartRaise(Code, 0).Predicting(() => _ran.Add("two")).Rollback(() => _ran.Add("undo two"));

        _predictions.RollBackRefused(Code);

        Assert.Equal(["one", "two", "undo one", "undo two"], _ran);
    }

    /// <summary>Raising without guessing is the ordinary case, and must leave nothing behind to expire.</summary>
    [Fact]
    public void A_raise_that_guesses_nothing_holds_nothing()
    {
        _predictions.StartRaise(Code, 0);

        _predictions.RollBackExpired(PredictedRaiseRegistry.Timeout * 10);

        Assert.Empty(_ran);
        Assert.Equal(0, _predictions.PendingCount);
    }
}
