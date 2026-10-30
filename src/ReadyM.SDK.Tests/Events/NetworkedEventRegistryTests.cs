using System.Collections.Generic;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.Multiplayer.ECS;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Multiplayer.RPC;
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
/// What a host gets out of the registry once the mods are in: one coordinator per declared event, built by the
/// container from every handler set this side declared for it. Generated code never names a coordinator, so
/// this is the only place the two halves meet.
/// </summary>
public class NetworkedEventRegistryTests
{
    private readonly List<string> _ran = [];

    private readonly NetworkedEventRegistry _registry = new(NullLogger.Instance);

    private struct Knock : INetworkedEvent
    {
        public RelayMessageCode Code => RelayMessageCode.MinAnyCustomEvent;

        public void Serialize(NetDataWriter writer)
        {
        }
    }

    private sealed class Contract : INetworkedEventContract<Knock>
    {
        public RelayMessageCode Code => RelayMessageCode.MinAnyCustomEvent;

        public EventScope Scope => EventScope.Server;

        public Policy Policy => Policy.Anyone;

        public EntityHandle? Subject(in Knock networkEvent) => null;

        public bool TryArea(in Knock networkEvent, out AreaId area)
        {
            area = default;
            return false;
        }

        public bool TryCell(in Knock networkEvent, out FullCellId cell)
        {
            cell = default;
            return false;
        }

        public PlayerId Target(in Knock networkEvent) => PlayerId.Invalid;

        public bool TryRead(INetworkedEventEntityReader entities, NetDataReader reader, out Knock networkEvent)
        {
            networkEvent = default;

            return true;
        }

        public void WriteReason(NetDataWriter writer, object? reason)
        {
        }

        public object? ReadReason(NetDataReader reader) => null;
    }

    /// <summary>
    /// Stands in for what a mod generates. The container builds it, so it takes what it needs the way a real
    /// one takes its handler classes.
    /// </summary>
    private abstract class Handlers(Ran ran) : INetworkedEventHandlers<Knock>
    {
        public abstract string Mod { get; }

        public IReadOnlyList<string> After => [];

        public void Run(Stage stage, ref Knock networkEvent, EventContext context)
        {
            if (stage == Stage.Apply)
                ran.Steps.Add(Mod);
        }
    }

    private sealed class Owner(Ran ran) : Handlers(ran)
    {
        public override string Mod => "Owner";
    }

    private sealed class Rules(Ran ran) : Handlers(ran)
    {
        public override string Mod => "Rules";
    }

    /// <summary>Registered so the container can hand the same list to whatever it builds.</summary>
    private sealed class Ran(List<string> steps)
    {
        public List<string> Steps { get; } = steps;
    }

    private sealed class Container : DependencyContainerBase;

    private Container Registered(
        RpcSide side, INetworkedEventCoordinatorRegistration registration, int declarations = 1)
    {
        var container = new Container();

        container.Init();
        container.RegisterSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        container.RegisterSingleton<ILogger>(NullLogger.Instance);
        container.RegisterSingleton(new Ran(_ran));

        var api = new FakeRelay().CreateEntityApi();
        var rpc = new FakeRpcRelay().CreateRpcApi();
        container.RegisterSingleton<IEntityApi>(api);
        container.RegisterSingleton(rpc);
        container.RegisterSingleton<IRpcClient>(new FakeRpcClient());
        container.RegisterSingleton<INetworkedEventEntityReader>(
            new NetworkedEventEntityReader(new RpcEntities(api, NullLogger.Instance)));
        container.RegisterSingleton(new PredictedRaiseRegistry(new GuessedValueRegistry(new PredictionDeltaHooks())));
        container.RegisterSingleton<ServerNetworkedEventScopes>();
        container.RegisterSingleton<ServerNetworkedEventPolicy>();
        container.RegisterSingleton(new ServerNetworkedEventSender(rpc, new ServerEntities(api), new FakePlayers()));

        for (var i = 0; i < declarations; i++)
        {
            _registry.Declare<Knock>(new Contract());
            _registry.AddHandlers<Owner, Knock>(RpcSide.Server);
            _registry.AddHandlers<Rules, Knock>(RpcSide.Server);
        }

        _registry.RegisterAll(container, side, registration);

        return container;
    }

    private Container OnServer() => Registered(RpcSide.Server, new ServerNetworkedEventCoordinatorRegistration());

    [Fact]
    public void A_declared_event_gets_one_coordinator_the_container_can_build()
    {
        using var container = OnServer();

        Assert.Same(
            container.Resolve<ServerNetworkedEventCoordinator<Knock>>(), container.Resolve<ServerNetworkedEventCoordinator<Knock>>());
    }

    /// <summary>
    /// The whole point of one coordinator rather than one dispatcher per mod: a raise reaches every mod's
    /// handlers. Resolving a dispatcher per mod handed back whichever one the container happened to pick.
    /// </summary>
    [Fact]
    public void A_raise_reaches_every_mods_handlers()
    {
        using var container = OnServer();

        container.Resolve<ServerNetworkedEventCoordinator<Knock>>().Raise(default, PlayerId.Server);

        Assert.Equal(["Owner", "Rules"], _ran);
    }

    /// <summary>Starting it is what makes it listen, so a host that starts its services must be able to.</summary>
    [Fact]
    public void A_coordinator_starts_with_the_containers_other_services()
    {
        using var container = OnServer();

        container.StartHostedServices();

        Assert.NotNull(container.Resolve<ServerNetworkedEventCoordinator<Knock>>());
    }

    /// <summary>An assembly loaded into two contexts declares its events twice.</summary>
    [Fact]
    public void Declaring_everything_twice_still_raises_each_handler_once()
    {
        using var container = Registered(
            RpcSide.Server, new ServerNetworkedEventCoordinatorRegistration(), declarations: 2);

        container.Resolve<ServerNetworkedEventCoordinator<Knock>>().Raise(default, PlayerId.Server);

        Assert.Equal(["Owner", "Rules"], _ran);
    }

    /// <summary>
    /// A client gets a coordinator for the same event, from the same declaration, because the Common project
    /// that declared it is on both machines. It has no server handler set in it: a Validate running on a
    /// client is the thing the stages exist to prevent.
    /// </summary>
    [Fact]
    public void A_client_gets_a_coordinator_without_the_servers_handlers()
    {
        using var container = Registered(RpcSide.Client, new ClientNetworkedEventCoordinatorRegistration());

        container.StartHostedServices();

        Assert.NotNull(container.Resolve<ClientNetworkedEventCoordinator<Knock>>());
        Assert.Empty(container.ResolveAll<INetworkedEventHandlers<Knock>>());
    }
}
