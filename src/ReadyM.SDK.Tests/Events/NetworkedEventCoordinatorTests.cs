using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.Multiplayer.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Events;
using ReadyM.SDK.Server.Entities;
using ReadyM.SDK.Server.Events;
using ReadyM.SDK.TestHarness;
using Xunit;

namespace ReadyM.SDK.Tests.Events;

/// <summary>
/// One event, several mods. The coordinator is the thing that makes that work: it runs a stage across every
/// mod before the next stage starts, so a second mod's Validate can still refuse what the first one accepted,
/// and it checks the policy and fans out once however many mods are listening.
/// </summary>
public class NetworkedEventCoordinatorTests
{
    private const RelayMessageCode Wire = (RelayMessageCode)ushort.MaxValue;

    private static readonly PlayerId Me = new(2);

    private static readonly PlayerId Somebody = new(5);

    private readonly List<string> _ran = [];

    /// <summary>What a mod's generated handler set is, as the coordinator sees it.</summary>
    private sealed class Handlers(string name, List<string> ran, params string[] after)
        : INetworkedEventHandlers<Blow>
    {
        /// <summary>The stage this one refuses in, and the reason it gives.</summary>
        public (Stage Stage, object? Reason)? Refuses { get; init; }

        /// <summary>What this mod's Filter adds to the damage, so a rewrite can be seen downstream.</summary>
        public int Adds { get; init; }

        /// <summary>What each Apply was told about whether this machine is the client that raised the event.</summary>
        public List<bool> AppliedOnRaisingClient { get; } = [];

        public string Mod => name;

        public IReadOnlyList<string> After => after;

        public void Run(Stage stage, ref Blow e, EventContext ctx)
        {
            ran.Add($"{name}:{stage}:{e.Damage}");

            if (stage == Stage.Apply)
                AppliedOnRaisingClient.Add(ctx.IsRaisingClient);

            if (stage == Stage.Filter)
                e.Damage += Adds;

            if (Refuses is { } refusal && refusal.Stage == stage)
                ctx.Cancellation.Cancel(refusal.Reason);
        }
    }

    private struct Blow : INetworkedEvent
    {
        public int Damage { get; set; }

        public RelayMessageCode Code => Wire;

        public void Serialize(NetDataWriter writer) => writer.Put(Damage);
    }

    /// <summary>Stands in for what the Common project generates beside the event struct.</summary>
    private sealed class Contract(EventScope scope, Policy policy) : INetworkedEventContract<Blow>
    {
        public RelayMessageCode Code => Wire;

        public EventScope Scope => scope;

        public Policy Policy => policy;

        public EntityHandle? Subject(in Blow e) => null;

        public bool TryArea(in Blow e, out AreaId area)
        {
            area = default;
            return false;
        }

        public bool TryCell(in Blow e, out FullCellId cell)
        {
            cell = default;
            return false;
        }

        public PlayerId Target(in Blow e) => PlayerId.Invalid;

        public bool TryRead(INetworkedEventEntityReader entities, NetDataReader reader, out Blow e)
        {
            e = new Blow { Damage = reader.GetInt() };

            return true;
        }

        public void WriteReason(NetDataWriter writer, object? reason) => writer.Put((int)(reason ?? 0));

        public object? ReadReason(NetDataReader reader) => reader.GetInt();
    }

    private readonly FakeRelay _world = new();

    private readonly FakeRpcRelay _rpc = new();

    private INetworkedEventEntityReader Reader()
        => new NetworkedEventEntityReader(new RpcEntities(_world.CreateEntityApi(), NullLogger.Instance));

    /// <summary>A server-scoped event, so nothing is sent but refusals.</summary>
    private ServerNetworkedEventCoordinator<Blow> OnServer(Policy policy, params Handlers[] mods)
    {
        var api = _world.CreateEntityApi();
        var rpc = _rpc.CreateRpcApi();
        var scopes = new ServerNetworkedEventScopes(api);

        return new ServerNetworkedEventCoordinator<Blow>(
            new Contract(EventScope.Server, policy),
            mods,
            rpc,
            Reader(),
            new ServerNetworkedEventPolicy(scopes),
            scopes,
            new ServerNetworkedEventSender(rpc, new ServerEntities(api), new FakePlayers()));
    }

    private ServerNetworkedEventCoordinator<Blow> OnServer(params Handlers[] mods) => OnServer(Policy.Anyone, mods);

    private (ClientNetworkedEventCoordinator<Blow> Coordinator, FakeRpcClient Relay) OnClient(
        PredictedRaiseRegistry predictions, params Handlers[] mods)
    {
        var relay = new FakeRpcClient();
        var coordinator = new ClientNetworkedEventCoordinator<Blow>(
            new Contract(EventScope.Global, Policy.Anyone), mods, relay, Reader(), predictions);

        coordinator.OnScopeStart();

        return (coordinator, relay);
    }

    private (ClientNetworkedEventCoordinator<Blow> Coordinator, FakeRpcClient Relay) OnClient(params Handlers[] mods)
        => OnClient(new PredictedRaiseRegistry(new GuessedValueRegistry(new PredictionDeltaHooks())), mods);

    /// <summary>What the server sends: the header, then the event.</summary>
    private static NetDataWriter Arriving(bool refused, PlayerId origin, int damage, params object[] refusal)
    {
        var message = new NetDataWriter();
        new NetworkedEventHeader(refused, origin).Write(message);
        message.Put(damage);

        foreach (var part in refusal)
        {
            if (part is string mod)
                message.Put(mod);
            else
                message.Put((int)part);
        }

        return message;
    }

    // -- only the two stages before the effect may refuse ----------------------------------------------

    /// <summary>
    /// Apply is the effect, and refusing there would tell the origin "refused" about something already done. It
    /// used to be ignored without a word, which is worse: the handler believed it had stopped the event.
    /// </summary>
    [Theory]
    [InlineData(Stage.Apply)]
    [InlineData(Stage.Observe)]
    public void Refusing_after_the_event_has_been_accepted_says_so(Stage stage)
    {
        var late = new Handlers("Late", _ran) { Refuses = (stage, null) };

        var thrown = Assert.Throws<InvalidOperationException>(() => OnServer(late).Raise(default, PlayerId.Server));

        Assert.Contains($"Stage.{stage}", thrown.Message);
        Assert.Contains("Validate", thrown.Message);
    }

    /// <summary>Rejected runs because the event was refused already, so refusing it again means nothing.</summary>
    [Fact]
    public void Refusing_in_rejected_says_so()
    {
        var refuser = new Handlers("Refuser", _ran) { Refuses = (Stage.Validate, null) };
        var late = new Handlers("Late", _ran) { Refuses = (Stage.Rejected, null) };

        var thrown = Assert.Throws<InvalidOperationException>(
            () => OnServer(refuser, late).Raise(default, PlayerId.Server));

        Assert.Contains("Stage.Rejected", thrown.Message);
    }

    /// <summary>The policy's own refusal comes before any stage, and is not a handler refusing late.</summary>
    [Fact]
    public void A_refusal_before_any_stage_is_still_allowed()
    {
        var refuser = new Handlers("Refuser", _ran) { Refuses = (Stage.Filter, null) };

        OnServer(refuser).Raise(default, PlayerId.Server);

        Assert.DoesNotContain("Refuser:Apply:0", _ran);
    }

    // -- one stage at a time, across every mod ---------------------------------------------------------

    /// <summary>
    /// The whole point of the coordinator. Run mod by mod instead and a second mod's Validate would be asked
    /// after the first mod had already applied the event, which is a rule checked too late to mean anything.
    /// </summary>
    [Fact]
    public void A_stage_runs_across_every_mod_before_the_next_stage_starts()
    {
        OnServer(new Handlers("A", _ran), new Handlers("B", _ran)).Raise(default, PlayerId.Server);

        Assert.Equal(
            [
                "A:Validate:0", "B:Validate:0",
                "A:Filter:0", "B:Filter:0",
                "A:Apply:0", "B:Apply:0",
                "A:Observe:0", "B:Observe:0",
            ],
            _ran);
    }

    /// <summary>A client runs the two stages that reach it, and neither of the two that never leave the server.</summary>
    [Fact]
    public void A_client_runs_apply_and_observe_across_every_mod_and_nothing_else()
    {
        var (_, relay) = OnClient(new Handlers("A", _ran), new Handlers("B", _ran));

        relay.Deliver(Wire, Arriving(false, Somebody, 3));

        Assert.Equal(["A:Apply:3", "B:Apply:3", "A:Observe:3", "B:Observe:3"], _ran);
    }

    /// <summary>What SkipOnRaisingClient reads, so an Apply the raising client already did does not happen twice.</summary>
    [Fact]
    public void An_event_this_client_raised_comes_back_marked_as_raised_here()
    {
        var mod = new Handlers("A", _ran);
        var (_, relay) = OnClient(mod);
        relay.PlayerId = Me;

        relay.Deliver(Wire, Arriving(false, Me, 3));
        relay.Deliver(Wire, Arriving(false, Somebody, 3));

        Assert.Equal([true, false], mod.AppliedOnRaisingClient);
    }

    /// <summary>The server is no client, so nothing it applies is skipped, whoever raised the event.</summary>
    [Fact]
    public void Nothing_the_server_applies_is_marked_as_raised_on_a_client()
    {
        var mod = new Handlers("A", _ran);
        var server = OnServer(mod);

        server.Raise(default, PlayerId.Server);
        server.Raise(default, Somebody);

        Assert.Equal([false, false], mod.AppliedOnRaisingClient);
    }

    /// <summary>A filter's rewrite is what every later stage, in every mod, is given.</summary>
    [Fact]
    public void What_one_mods_filter_rewrote_is_what_the_next_mod_applies()
    {
        OnServer(new Handlers("A", _ran) { Adds = 5 }, new Handlers("B", _ran) { Adds = 2 })
            .Raise(default, PlayerId.Server);

        Assert.Equal(["A:Apply:7", "B:Apply:7"], _ran.Where(step => step.Contains(":Apply:")));
    }

    // -- refusing --------------------------------------------------------------------------------------

    /// <summary>
    /// An anti-cheat mod's whole job: refuse what the event's own mod was happy with. That only works if its
    /// Validate runs before the owner's Apply, which is what a per-mod ladder cannot do.
    /// </summary>
    [Fact]
    public void A_second_mods_validate_stops_the_first_mods_apply()
    {
        OnServer(
                new Handlers("Owner", _ran),
                new Handlers("Rules", _ran) { Refuses = (Stage.Validate, 9) })
            .Raise(default, PlayerId.Server);

        Assert.Equal(["Owner:Validate:0", "Rules:Validate:0", "Owner:Rejected:0", "Rules:Rejected:0"], _ran);
    }

    /// <summary>Every mod hears that it was refused, not only the one whose handler refused it.</summary>
    [Fact]
    public void A_refusal_reaches_every_mods_rejected_handlers()
    {
        OnServer(new Handlers("A", _ran) { Refuses = (Stage.Validate, 1) }, new Handlers("B", _ran))
            .Raise(default, PlayerId.Server);

        Assert.Equal(["A:Validate:0", "A:Rejected:0", "B:Rejected:0"], _ran);
    }

    [Fact]
    public void A_refusal_in_filter_still_stops_apply()
    {
        OnServer(new Handlers("A", _ran) { Refuses = (Stage.Filter, 1) }, new Handlers("B", _ran))
            .Raise(default, PlayerId.Server);

        Assert.DoesNotContain(_ran, step => step.Contains(":Apply:"));
        Assert.Contains("B:Validate:0", _ran);
    }

    /// <summary>A client is told which mod refused it, so a Rejected handler can say so.</summary>
    [Fact]
    public void A_client_told_of_a_refusal_runs_rejected_alone_and_knows_who_refused()
    {
        var mods = new[] { new Handlers("A", _ran), new Handlers("B", _ran) };
        var (_, relay) = OnClient(mods);

        relay.Deliver(Wire, Arriving(true, Me, 4, "AntiCheat", 7));

        Assert.Equal(["A:Rejected:4", "B:Rejected:4"], _ran);
    }

    // -- the policy ------------------------------------------------------------------------------------

    /// <summary>
    /// Asked once, before any mod sees the event. A per-mod ladder asks it once per mod, which is the same
    /// answer at a cost that grows with how many mods are installed.
    /// </summary>
    [Fact]
    public void A_client_that_may_not_raise_it_gets_no_stage_but_rejected()
    {
        OnServer(Policy.ServerAuthoritative, new Handlers("A", _ran), new Handlers("B", _ran))
            .Dispatch(default, Somebody, checkPolicy: true);

        Assert.Equal(["A:Rejected:0", "B:Rejected:0"], _ran);
    }

    /// <summary>The server vouches for what it raises, so a bot can act as a player the policy would refuse.</summary>
    [Fact]
    public void The_server_raising_as_a_player_is_not_asked_the_policy()
    {
        OnServer(Policy.ServerAuthoritative, new Handlers("A", _ran)).Raise(default, Somebody);

        Assert.Contains("A:Apply:0", _ran);
    }

    // -- the order mods run in -------------------------------------------------------------------------

    [Fact]
    public void A_mod_that_names_another_runs_behind_it_whatever_its_name_sorts_as()
    {
        OnServer(new Handlers("Second", _ran, "First"), new Handlers("First", _ran)).Raise(default, PlayerId.Server);

        Assert.Equal(["First:Apply:0", "Second:Apply:0"], _ran.Where(step => step.Contains(":Apply:")));
    }

    /// <summary>
    /// By name, not by the order the container handed them over in. That order is the order the mods loaded,
    /// which the server and a client do differently, and two mods applying one event in opposite orders on
    /// the two machines is a divergence nothing would report.
    /// </summary>
    [Fact]
    public void Mods_that_name_nobody_run_in_name_order()
    {
        OnServer(new Handlers("Z", _ran), new Handlers("A", _ran)).Raise(default, PlayerId.Server);

        Assert.Equal(["A:Apply:0", "Z:Apply:0"], _ran.Where(step => step.Contains(":Apply:")));
    }

    /// <summary>A client runs its mods in the same order the server ran them, or the two worlds diverge.</summary>
    [Fact]
    public void A_client_runs_its_mods_in_the_same_order()
    {
        var (_, relay) = OnClient(new Handlers("Second", _ran, "First"), new Handlers("First", _ran));

        relay.Deliver(Wire, Arriving(false, Somebody, 1));

        Assert.Equal(["First:Apply:1", "Second:Apply:1"], _ran.Where(step => step.Contains(":Apply:")));
    }

    // -- an event nobody here handles ------------------------------------------------------------------

    /// <summary>
    /// A client that raises an event it declares no handler for still has a coordinator, because the
    /// declaration is in the Common project both sides share. Without one, a refusal of its own event would
    /// arrive on a code nothing is listening to and its guess would stand until the deadline took it back.
    /// </summary>
    [Fact]
    public void A_client_with_no_handlers_still_takes_its_guess_back_on_a_refusal()
    {
        var predictions = new PredictedRaiseRegistry(new GuessedValueRegistry(new PredictionDeltaHooks()));
        var (_, relay) = OnClient(predictions);

        predictions.StartRaise(Wire, 0).Predicting(() => _ran.Add("guess")).Rollback(() => _ran.Add("undo"));

        relay.Deliver(Wire, Arriving(true, Me, 1, "Someone", 0));

        Assert.Equal(["guess", "undo"], _ran);
    }

    /// <summary>The server's refusal goes back to the player whose event it was, and to nobody else.</summary>
    [Fact]
    public void A_refusal_is_sent_to_the_origin()
    {
        OnServer(Policy.ServerAuthoritative, new Handlers("A", _ran)).Dispatch(default, Somebody, checkPolicy: true);

        Assert.Equal([Somebody], _rpc.Sent.Select(sent => sent.Player));
    }

    [Fact]
    public void A_disposed_client_coordinator_stops_listening()
    {
        var (coordinator, relay) = OnClient(new Handlers("A", _ran));

        Assert.Equal(1, relay.Subscriptions);

        coordinator.Dispose();

        Assert.Equal(0, relay.Subscriptions);
    }

    [Fact]
    public void A_server_with_no_handlers_for_an_event_still_dispatches_it()
    {
        var coordinator = OnServer();

        coordinator.Raise(default, PlayerId.Server);

        Assert.Empty(_ran);
    }
}
