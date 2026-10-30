using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.Multiplayer.ECS;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Events;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Events;
using ReadyM.SDK.Server.Entities;
using ReadyM.SDK.Server.Events;
using ReadyM.SDK.TestHarness;
using Xunit;

namespace ReadyM.SDK.Tests.Events;

/// <summary>
/// A raise belongs on the ECS thread, where the entities it names and the guesses made at them live. Raised from
/// anywhere else it races them, and the way that showed up was a guess whose deadline was sometimes lost. So a
/// raise off the thread is refused on the spot, saying where it should have been made instead.
/// </summary>
public class NetworkedEventThreadTests : IDisposable
{
    private readonly Store _world = new(new EntityStore(), NullLogger.Instance, Array.Empty<IArchetypeRegistration>());

    private readonly FakeRpcClient _relay = new();

    private readonly TestContainer _server = new();

    private readonly List<string> _ran = [];

    public NetworkedEventThreadTests()
    {
        // The test's own thread stands in for the ECS thread, the way a host marks its own before ticking
        _world.SetThread(Thread.CurrentThread);

        // A server mod asks the host through its entity api, which the fake relay answers for the thread that built it
        var world = new FakeRelay();
        var api = world.CreateEntityApi();
        var rpc = new FakeRpcRelay().CreateRpcApi();
        var scopes = new ServerNetworkedEventScopes(api);

        _server.Init();
        _server.RegisterSingleton<IEntityApi>(api);
        _server.RegisterSingleton(new ServerNetworkedEventCoordinator<Ping>(
            new PingContract(),
            [new Handlers(_ran)],
            rpc,
            new NetworkedEventEntityReader(new RpcEntities(api, NullLogger.Instance)),
            new ServerNetworkedEventPolicy(scopes),
            scopes,
            new ServerNetworkedEventSender(rpc, new ServerEntities(api), new FakePlayers())));
    }

    public void Dispose() => _server.Dispose();

    private INetworkedEvents Client() => new ClientNetworkedEvents(_relay, new PredictedRaiseRegistry(new GuessedValueRegistry(new PredictionDeltaHooks())), _world);

    private INetworkedEvents Server() => new ServerNetworkedEvents(_server, _server.Resolve<IEntityApi>());

    /// <summary>Runs the call on a thread of its own and hands back what it threw, as a callback from the game would.</summary>
    private static Exception? Elsewhere(Action raise)
    {
        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                raise();
            }
            catch (Exception exception)
            {
                thrown = exception;
            }
        }) { Name = "Game callback" };

        thread.Start();
        thread.Join();

        return thrown;
    }

    [Fact]
    public void A_client_raise_on_the_ECS_thread_goes_to_the_server()
    {
        Client().Raise(new Ping());

        Assert.Equal(1, _relay.Sent);
    }

    [Fact]
    public void A_client_raise_off_the_ECS_thread_is_refused_and_nothing_is_sent()
    {
        var thrown = Elsewhere(() => Client().Raise(new Ping()));

        AssertRefused(thrown);
        Assert.Equal(0, _relay.Sent);
    }

    [Fact]
    public void A_server_raise_on_the_ECS_thread_runs_the_event()
    {
        Server().Raise(new Ping());

        Assert.Equal(["Apply"], _ran);
    }

    [Fact]
    public void A_server_raise_off_the_ECS_thread_is_refused_and_nothing_runs()
    {
        var thrown = Elsewhere(() => Server().Raise(new Ping()));

        AssertRefused(thrown);
        Assert.Empty(_ran);
    }

    [Fact]
    public void Raising_as_another_player_off_the_ECS_thread_is_refused_the_same_way()
    {
        var thrown = Elsewhere(() => Server().RaiseAs(new PlayerId(3), new Ping()));

        AssertRefused(thrown);
        Assert.Empty(_ran);
    }

    /// <summary>It names the event and the thread it came from, and says what to do about it.</summary>
    private static void AssertRefused(Exception? thrown)
    {
        var refused = Assert.IsType<EventRaisedOffEcsThreadException>(thrown);

        Assert.Contains("Ping", refused.Message);
        Assert.Contains("Game callback", refused.Message);
        Assert.Contains("ECS thread", refused.Message);
        Assert.Contains("Schedule", refused.Message);
    }

    private struct Ping : INetworkedEvent
    {
        public RelayMessageCode Code => (RelayMessageCode)ushort.MaxValue;

        public void Serialize(NetDataWriter writer)
        {
        }
    }

    private sealed class PingContract : INetworkedEventContract<Ping>
    {
        public RelayMessageCode Code => (RelayMessageCode)ushort.MaxValue;

        public EventScope Scope => EventScope.Server;

        public Policy Policy => Policy.Anyone;

        public EntityHandle? Subject(in Ping networkEvent) => null;

        public bool TryArea(in Ping networkEvent, out AreaId area)
        {
            area = default;
            return false;
        }

        public bool TryCell(in Ping networkEvent, out FullCellId cell)
        {
            cell = default;
            return false;
        }

        public PlayerId Target(in Ping networkEvent) => PlayerId.Invalid;

        public bool TryRead(INetworkedEventEntityReader entities, NetDataReader reader, out Ping networkEvent)
        {
            networkEvent = default;
            return true;
        }

        public void WriteReason(NetDataWriter writer, object? reason)
        {
        }

        public object? ReadReason(NetDataReader reader) => null;
    }

    private sealed class Handlers(List<string> ran) : INetworkedEventHandlers<Ping>
    {
        public string Mod => "Pinger";

        public IReadOnlyList<string> After => [];

        public void Run(Stage stage, ref Ping networkEvent, EventContext context)
        {
            if (stage == Stage.Apply)
                ran.Add("Apply");
        }
    }

    private sealed class TestContainer : DependencyContainerBase;
}
