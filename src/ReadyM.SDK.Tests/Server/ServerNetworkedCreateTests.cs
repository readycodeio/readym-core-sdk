using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Server.Entities;
using ReadyM.SDK.Tests.Server.Fixtures;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// Whether an entity the server makes crosses the wire, and who it belongs to when it does.
/// </summary>
/// <remarks>
/// Read off the fake relay rather than through the SDK, because the SDK names none of it: a mod
/// asks for an entity of a shape, and what the relay was asked to do follows from what the
/// archetype declared.
/// </remarks>
public class ServerNetworkedCreateTests : ServerSdkTest
{
    public ServerNetworkedCreateTests()
    {
        // What a game does for each of its shapes. Beacon replicates, so without this the create is
        // refused rather than landing on an id only this side knows.
        Relay.Bind(typeof(Beacon), Relay.RegisterArchetype(ComponentsOf<Beacon>().Types));
        Relay.Bind(typeof(Waypoint), Relay.RegisterArchetype(ComponentsOf<Waypoint>().Types));
    }

    private static readonly PlayerId Player = new(7);

    private TestArea Area()
    {
        var area = Spawn<TestArea>();

        area.AreaId = 1;

        return area;
    }

    // -- what decides it ---------------------------------------------------------------------------

    /// <summary>Nothing about a guard leaves this machine, so the relay is never asked to name it.</summary>
    [Fact]
    public void A_shape_with_nothing_to_send_is_created_local()
        => Assert.Null(Relay.NetworkedOf(IdentityOf(Entities.Create<Guard>())));

    /// <summary>Beacon is declared replicated, so the entity has to be one the client can name.</summary>
    [Fact]
    public void A_shape_that_replicates_is_created_networked()
        => Assert.NotNull(Relay.NetworkedOf(IdentityOf(Entities.Create<Beacon>())));

    /// <summary>Probe holds what Beacon does and stays on the server, without a binding to say where.</summary>
    [Fact]
    public void A_local_archetype_stays_local_whatever_it_includes()
        => Assert.Null(Relay.NetworkedOf(IdentityOf(Entities.Create<Probe>())));

    /// <summary>Nothing Waypoint holds is sent, and the entity still is: its presence is the message.</summary>
    [Fact]
    public void A_replicated_archetype_with_nothing_that_sends_is_networked()
        => Assert.NotNull(Relay.NetworkedOf(IdentityOf(Entities.Create<Waypoint>())));

    /// <summary>A query asks for components, not for how the entity came to have them, so it finds both.</summary>
    [Fact]
    public void A_query_over_a_replicated_mixin_finds_local_entities_too()
    {
        Entities.Create<Beacon>();
        Entities.Create<Probe>();

        var found = 0;

        foreach (var _ in Entities.Query<Telemetry>())
            found++;

        Assert.Equal(2, found);
    }

    /// <summary>It lands on the archetype the game registered, not on a fresh one only the server knows.</summary>
    [Fact]
    public void A_networked_entity_is_made_of_the_archetype_the_game_bound()
    {
        var bound = Relay.RegisterArchetype(ComponentsOf<Beacon>().Types);

        Assert.Equal(bound, Relay.NetworkedOf(IdentityOf(Entities.Create<Beacon>()))!.Value.Archetype);
    }

    /// <summary>
    /// A replicating shape no game registered an archetype for is refused where it is asked for.
    /// </summary>
    /// <remarks>
    /// Registering one here would hand back an id only the server holds, and the create would reach
    /// a client that cannot resolve it, which is a failure a long way from its cause.
    /// </remarks>
    [Fact]
    public void A_replicating_shape_the_game_never_bound_cannot_be_created()
    {
        var refused = Assert.Throws<InvalidOperationException>(() => Entities.Create<Unbound>());

        Assert.Contains(typeof(Unbound).FullName!, refused.Message);
    }

    /// <summary>Unless the SDK registered one under its name as the mods started, which a client does too.</summary>
    [Fact]
    public void A_replicating_shape_the_game_never_bound_is_registered_as_the_mods_start()
    {
        // The pass takes every replicated shape this assembly declares, the client's fixtures too, so the relay has
        // to know all of what they hold.
        foreach (var (_, _, components) in ArchetypeContributions.Replicated())
        foreach (var component in components.Types)
            Relay.RegisterUnnamed(component);

        Api.RegisterReplicatedShapes();

        Assert.NotNull(Relay.NetworkedOf(IdentityOf(Entities.Create<Unbound>())));
    }

    // -- who owns it -------------------------------------------------------------------------------

    /// <summary>Null is the server's own, which is what a server making an entity means.</summary>
    [Fact]
    public void A_networked_entity_is_the_servers_unless_a_player_is_named()
        => Assert.Null(Relay.NetworkedOf(IdentityOf(Entities.Create<Beacon>()))!.Value.Owner);

    /// <summary>
    /// Naming an owner is the server's to do, and it is the half of the surface a client is not given.
    /// </summary>
    /// <remarks>
    /// Asked of the shared base rather than of the client, because that is the whole of what a client
    /// gets: an overload taking an owner appearing here would reach one.
    /// </remarks>
    [Fact]
    public void Only_the_servers_entities_service_lets_an_owner_be_named()
    {
        var onBase = typeof(IEntitiesBase).GetMethods()
            .Where(method => method.Name == nameof(IEntitiesBase.Create))
            .SelectMany(method => method.GetParameters())
            .Any(parameter => parameter.ParameterType == typeof(PlayerId));

        Assert.False(onBase);
    }

    /// <summary>An owner is named alongside a scope or not at all, so there is no unscoped overload to reach.</summary>
    [Fact]
    public void An_owner_is_only_ever_named_with_a_scope()
    {
        var withOwner = typeof(IEntities).GetMethods()
            .Where(method => method.Name == nameof(IEntities.Create))
            .Select(method => method.GetParameters())
            .Where(parameters => parameters.Any(p => p.ParameterType == typeof(PlayerId)));

        Assert.All(withOwner, parameters
            => Assert.Contains(parameters, p => p.ParameterType == typeof(Scope)));
    }

    // -- scope is a separate question --------------------------------------------------------------

    [Fact]
    public void A_networked_entity_keeps_the_scope_it_was_made_in()
    {
        var area = Area();
        var made = Relay.NetworkedOf(IdentityOf(Entities.Create<Beacon>(area)))!.Value;

        // By id: RawEntity refuses Equals(object) so a boxed comparison throws.
        Assert.Equal(IdentityOf(area).Id, made.Scope!.Value.Id);
    }

    /// <summary>Scope and owner are decided apart, so a scoped create still carries the owner it was given.</summary>
    [Fact]
    public void A_scoped_networked_entity_keeps_its_owner_too()
    {
        var area = Area();
        var made = Relay.NetworkedOf(IdentityOf(Entities.Create<Beacon>(area, Player)))!.Value;

        Assert.Equal(Player, made.Owner);
        Assert.Equal(IdentityOf(area).Id, made.Scope!.Value.Id);
    }

    /// <summary>A local shape in a scope is still local: the scope says where it lives, not who hears of it.</summary>
    [Fact]
    public void A_local_entity_in_a_scope_stays_local()
        => Assert.Null(Relay.NetworkedOf(IdentityOf(Entities.Create<Guard>(Area()))));

    // -- going away --------------------------------------------------------------------------------

    /// <summary>Deleting one reaches the relay, which is the only thing that can tell the other side.</summary>
    [Fact]
    public void Deleting_a_networked_entity_reaches_the_relay()
    {
        var beacon = Entities.Create<Beacon>();

        Relay.ResetCounters();
        Assert.True(Entities.Delete(beacon));
        Assert.Equal(1, Relay.DeleteCalls);
    }

    [Fact]
    public void And_the_entity_is_gone()
    {
        var beacon = Entities.Create<Beacon>();

        Entities.Delete(beacon);

        Assert.False(EntityHandle.Of(beacon).IsAlive());
    }
}
