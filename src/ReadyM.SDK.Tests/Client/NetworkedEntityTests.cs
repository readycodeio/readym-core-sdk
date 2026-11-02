using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Client.Archetypes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// Whether an entity is networked is what its archetype declared, and nothing else: not what it is
/// made of, and not what a mod asks for at the create.
/// </summary>
/// <remarks>
/// Read through MetadataComponent, which is what the networked half puts on an entity and what the
/// relay finds it by. The SDK never names it, so its presence is the only thing a test can ask.
/// </remarks>
public class NetworkedEntityTests : IDisposable
{
    private readonly TestContainer _container;
    private readonly LocalPlayer _player = new();

    public NetworkedEntityTests()
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

        _container.RegisterSingleton<IEntityApi, ClientEntityApi>();
        _container.RegisterSingleton<IEntities, ClientEntities>();

        World = _container.Resolve<Store>();
        World.SetThread(Thread.CurrentThread);

        Bind<Area>();
        Bind<Prop>();
        Bind<Rig>();
        Bind<Borrowed>();
        Bind<Signpost>();

        ArchetypeBindings.Use(_bound);

        Entities = _container.Resolve<IEntities>();
        Networked = _container.Resolve<INetworkedEntityManager>();
    }

    private Store World { get; }

    private IEntities Entities { get; }

    private INetworkedEntityManager Networked { get; }

    public void Dispose() => _container.Dispose();

    /// <summary>What a game does for each of its shapes, so a create lands on an archetype both sides name.</summary>
    private void Bind<T>() where T : struct, IArchetype
    {
        var components = ArchetypeRegistry.SetFor(typeof(T), default(T).Components);
        var builder = new ArchetypeBuilder();

        foreach (var type in components.Types)
            builder.Add(type);

        _bound[typeof(T).FullName!] = World.RegisterArchetype(builder);
    }

    private readonly Dictionary<string, ArchetypeId> _bound = [];

    // -- what decides it ---------------------------------------------------------------------------

    /// <summary>Nothing about a prop leaves this machine, so nothing has to name it on another one.</summary>
    [Fact]
    public void A_shape_with_nothing_to_send_is_created_local()
        => Assert.False(Has<MetadataComponent>(Entities.Create<Prop>()));

    /// <summary>Rig is declared replicated, so the entity has to be one the relay can name.</summary>
    [Fact]
    public void A_shape_that_replicates_is_created_networked()
        => Assert.True(Has<MetadataComponent>(Entities.Create<Rig>()));

    /// <summary>
    /// The same for a shape over a component the game declares. The SDK generated none of it, and it
    /// makes no difference: the archetype said so.
    /// </summary>
    [Fact]
    public void A_shape_over_a_component_the_game_sends_is_networked_as_well()
        => Assert.True(Has<MetadataComponent>(Entities.Create<Borrowed>()));

    /// <summary>Gauge carries the mixin Rig sends, and stays here anyway, without a binding to say where.</summary>
    [Fact]
    public void A_local_archetype_stays_local_whatever_it_includes()
        => Assert.False(Has<MetadataComponent>(Entities.Create<Gauge>()));

    /// <summary>Nothing Signpost holds is sent, and the entity still is: its presence is the message.</summary>
    [Fact]
    public void A_replicated_archetype_with_nothing_that_sends_is_networked()
        => Assert.True(Has<MetadataComponent>(Entities.Create<Signpost>()));

    /// <summary>A query asks for components, not for how the entity came to have them, so it finds both.</summary>
    [Fact]
    public void A_query_over_a_replicated_mixin_finds_local_entities_too()
    {
        Entities.Create<Rig>();
        Entities.Create<Gauge>();

        var found = 0;

        foreach (var _ in Entities.Query<Telemetry>())
            found++;

        Assert.Equal(2, found);
    }

    // -- who owns it -------------------------------------------------------------------------------

    /// <summary>A client makes entities of its own and nobody else's, so there is no owner to pass.</summary>
    [Fact]
    public void A_networked_entity_belongs_to_the_player_that_made_it()
    {
        var rig = Entities.Create<Rig>();

        Assert.Equal(_player.PlayerId, Owner(rig));
    }

    /// <summary>
    /// The rule is in the surface, not in a check: a client that cannot say an owner cannot get one
    /// wrong. The server's own IEntities is where naming one belongs.
    /// </summary>
    [Fact]
    public void A_client_is_given_no_way_to_name_an_owner()
    {
        var takesAnOwner = typeof(IEntities).GetMethods()
            .Where(method => method.Name == nameof(IEntities.Create))
            .SelectMany(method => method.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(PlayerId)
                              || parameter.ParameterType == typeof(PlayerId?));

        Assert.False(takesAnOwner);
    }

    // -- scope is a separate question --------------------------------------------------------------

    /// <summary>
    /// Being in a scope and being networked are decided apart: a scope says where the entity lives,
    /// the components say whether anyone else hears about it.
    /// </summary>
    [Fact]
    public void A_networked_entity_keeps_the_scope_it_was_made_in()
    {
        var area = Entities.Create<Area>();
        var rig = Entities.Create<Rig>(area);

        Assert.True(Has<MetadataComponent>(rig));
        Assert.True(Has<InScopeComponent>(rig));
    }

    [Fact]
    public void A_local_entity_keeps_it_too()
    {
        var area = Entities.Create<Area>();
        var prop = Entities.Create<Prop>(area);

        Assert.False(Has<MetadataComponent>(prop));
        Assert.True(Has<InScopeComponent>(prop));
    }

    // -- going away --------------------------------------------------------------------------------

    /// <summary>
    /// Deleting a networked entity has to reach the network, not just the store: the other side is
    /// holding a copy and nothing else will tell it to let go.
    /// </summary>
    [Fact]
    public void Deleting_a_networked_entity_tells_the_network()
    {
        var rig = Entities.Create<Rig>();
        var netId = NetIdOf(rig);

        Entities.Delete(rig);

        Assert.True(Networked.IsNetworkEntityDeleted(netId));
    }

    [Fact]
    public void And_the_entity_is_gone()
    {
        var rig = Entities.Create<Rig>();

        Entities.Delete(rig);

        Assert.False(EntityHandle.Of(rig).IsAlive());
    }

    /// <summary>A local one has nobody to tell, and is simply gone.</summary>
    [Fact]
    public void Deleting_a_local_entity_just_removes_it()
    {
        var prop = Entities.Create<Prop>();

        Entities.Delete(prop);

        Assert.False(EntityHandle.Of(prop).IsAlive());
    }

    // -- landing on the archetype the other side knows ---------------------------------------------

    /// <summary>
    /// Archetype ids travel on the wire and are positional, so a create has to land on the one the
    /// game registered rather than mint a fresh one only this side can name.
    /// </summary>
    [Fact]
    public void A_networked_entity_is_made_of_the_archetype_the_game_bound()
    {
        var rig = Entities.Create<Rig>();

        Assert.Equal(_bound[typeof(Rig).FullName!], Archetype(rig));
    }

    /// <summary>
    /// Minting one here would reach the server under a name only this side knows, so it is refused
    /// at the create rather than at the entity's first hop.
    /// </summary>
    [Fact]
    public void A_replicating_shape_the_game_never_bound_cannot_be_created()
    {
        var refused = Assert.Throws<InvalidOperationException>(() => Entities.Create<Unbound>());

        Assert.Contains(typeof(Unbound).FullName!, refused.Message);
    }

    /// <summary>Unless the SDK's pass registered one under its name, as the server does with the same shapes.</summary>
    [Fact]
    public void A_replicating_shape_the_game_never_bound_is_registered_by_the_sdk()
    {
        foreach (var (shape, archetype) in ClientArchetypes.RegisterReplicated(World, _bound))
            _bound[shape] = archetype;

        ArchetypeBindings.Use(_bound);

        Assert.True(World.TryGetArchetypeId(typeof(Unbound).FullName!, out var registered));
        Assert.Equal(registered, Archetype(Entities.Create<Unbound>()));
    }

    // -- reading what the SDK does not name --------------------------------------------------------

    private bool Has<TComponent>(object shape) where TComponent : struct, IComponent
        => World.GetEntityByRawEntity(RawOf(shape)).HasComponent<TComponent>();

    private PlayerId Owner(object shape)
        => World.GetEntityByRawEntity(RawOf(shape)).GetComponent<MetadataComponent>().Owner;

    private ArchetypeId Archetype(object shape)
        => World.GetEntityByRawEntity(RawOf(shape)).GetComponent<MetadataComponent>().Archetype;

    private NetworkId NetIdOf(object shape)
        => World.GetEntityByRawEntity(RawOf(shape)).GetComponent<MetadataComponent>().NetId;

    private static RawEntity RawOf(object shape)
        => shape switch
        {
            Rig rig => EntityHandle.Of(rig).RawEntity,
            Prop prop => EntityHandle.Of(prop).RawEntity,
            Borrowed borrowed => EntityHandle.Of(borrowed).RawEntity,
            Unbound unbound => EntityHandle.Of(unbound).RawEntity,
            Gauge gauge => EntityHandle.Of(gauge).RawEntity,
            Signpost signpost => EntityHandle.Of(signpost).RawEntity,
            _ => throw new ArgumentException($"No handle for {shape.GetType().Name}.", nameof(shape))
        };

    private sealed class LocalPlayer : IPlayerIdProvider
    {
        public PlayerId? PlayerId { get; } = new(7);
    }

    private sealed class TestContainer : DependencyContainerBase;
}
