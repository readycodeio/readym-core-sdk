using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Client.Archetypes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Events;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// A guess meeting the server's delta through the job that really applies deltas, on an entity this client
/// owns. The other prediction tests hand the hooks a delta themselves; this is what says the job calls them,
/// and in the order the guess depends on.
/// </summary>
public class PredictedDeltaTests : IDisposable
{
    private const RelayMessageCode Code = (RelayMessageCode)ushort.MaxValue;

    private readonly TestContainer _container;
    private readonly LocalPlayer _player = new();
    private readonly Dictionary<string, ArchetypeId> _bound = [];

    public PredictedDeltaTests()
    {
        _container = new TestContainer();
        _container.Init();

        _container.RegisterSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _container.RegisterSingleton<ILogger>(NullLogger.Instance);

        var store = new EntityStore();

        _container.RegisterSingleton(store);
        _container.RegisterSingleton(new Store(store, NullLogger.Instance, Array.Empty<IArchetypeRegistration>()));
        _container.RegisterSingleton<IPlayerIdProvider>(_player);
        _container.RegisterSingleton<INetworkedEntityManager, NetworkedEntityManager>();

        CreateHandlerRegistry.Use(_container);

        _container.RegisterSingleton<PredictionDeltaHooks>();
        _container.RegisterSingleton<GuessedValueRegistry>();
        _container.RegisterSingleton<PredictedRaiseRegistry>();
        _container.RegisterSingleton<IEntityApi, ClientEntityApi>();
        _container.RegisterSingleton<IEntities, ClientEntities>();

        var world = _container.Resolve<Store>();
        world.SetThread(Thread.CurrentThread);

        Bind<Settlement>(world);
        ArchetypeBindings.Use(_bound);

        Entities = _container.Resolve<IEntities>();
        Hooks = _container.Resolve<PredictionDeltaHooks>().For<LockdownComponent>();
        PredictedRaises = _container.Resolve<PredictedRaiseRegistry>();
        Job = new ApplyDeltaJob<LockdownComponent>(
            _container.Resolve<INetworkedEntityManager>(), _player, Hooks, NullLogger.Instance);
    }

    private IEntities Entities { get; }

    private PredictionDeltaHooks<LockdownComponent> Hooks { get; }

    private PredictedRaiseRegistry PredictedRaises { get; }

    private void Bind<T>(Store world) where T : struct, IArchetype
    {
        var components = ArchetypeRegistry.SetFor(typeof(T), default(T).Components);
        var builder = new ArchetypeBuilder();

        foreach (var type in components.Types)
            builder.Add(type);

        _bound[typeof(T).FullName!] = world.RegisterArchetype(builder);
    }

    private ApplyDeltaJob<LockdownComponent> Job { get; }

    public void Dispose() => _container.Dispose();

    private Settlement Town()
    {
        var town = Entities.Create<Settlement>();

        Component(town).ClearDirty();
        Component(town).ClearApiFlag();

        return town;
    }

    private static ref LockdownComponent Component(Settlement town)
        => ref EntityHandle.Of(town).GetComponent<LockdownComponent>();

    /// <summary>The server's delta for the town, carrying only what it changed, through the real job.</summary>
    private void Arrives(Settlement town, bool? active = null, int? hour = null)
    {
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
        writer.Put(EntityHandle.Of(town).GetComponent<MetadataComponent>().NetId);
        server.WriteDelta(writer);

        Job.Execute(new NetDataReader(writer.CopyData()));
    }

    [Fact]
    public void The_town_is_this_clients_own()
        => Assert.Equal(_player.PlayerId, EntityHandle.Of(Town()).GetComponent<MetadataComponent>().Owner);

    [Fact]
    public void A_delta_carrying_the_guessed_value_answers_the_guess()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));

        Arrives(town, active: true);
        PredictedRaises.RollBackExpired(PredictedRaiseRegistry.Timeout);

        Assert.True(town.LockdownActive);
        Assert.Null(Hooks.AfterDeltaRead);
    }

    /// <summary>
    /// The answer came from the server, so it is not sent back, and the game is told to show it: on an owned
    /// entity that is the mark of a change from the API, which the job puts on whatever the delta carried.
    /// </summary>
    [Fact]
    public void An_answered_guess_leaves_the_servers_value_for_the_game_and_nothing_to_send()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));
        Component(town).ClearApiFlag();

        Arrives(town, active: true);

        Assert.True(Component(town).ChangedFromApi);
        Assert.False(Component(town).IsDirty);
    }

    [Fact]
    public void A_delta_carrying_another_field_leaves_the_guess_and_the_deadline_puts_the_servers_value_back()
    {
        var town = Town();

        PredictedRaises.StartRaise(Code, 0).Predicting(() => town.PredictLockdownActive(true));

        Arrives(town, hour: 7);

        Assert.True(town.LockdownActive);
        Assert.Equal(7, town.LockdownHour);
        Assert.False(Component(town).IsDirty);

        PredictedRaises.RollBackExpired(PredictedRaiseRegistry.Timeout);

        Assert.False(town.LockdownActive);
        Assert.Equal(7, town.LockdownHour);
    }

    private sealed class LocalPlayer : IPlayerIdProvider
    {
        public PlayerId? PlayerId { get; } = new(7);
    }

    private sealed class TestContainer : DependencyContainerBase;
}
