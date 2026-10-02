using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// What a write through a replicated shape does on a client, where the component's policy decides
/// whether it lands and what it means. A refused write changes nothing and says so, which is the
/// half that would otherwise go unnoticed.
public class WritePolicyTests
{
    private readonly EntityStore _store = new();

    /// <param name="owned">Whether this client drives the value from its game, which is what the
    /// ownership policy answers for an entity it owns.</param>
    private Rig Spawn(bool owned)
    {
        var policy = new Policy { GameCopiesIn = owned, SetsFromApi = owned };
        var api = new ClientEntityApi(_store, NullLogger<ClientEntityApi>.Instance, new Lazy<IMappingPolicyDirectory>(() => new Directory(policy)));
        var entity = _store.CreateEntity(new TelemetryComponent(), new RigComponent());

        return Shaped<Rig>(new EntityHandle(entity.RawEntity, api));
    }

    private static T Shaped<T>(EntityHandle handle) where T : struct, IArchetypeQueryable
        => new() { Handle = handle };

    /// The same entity, carrying the mixin whose value is a collection.
    private Rig SpawnWithRoster(bool owned)
    {
        var policy = new Policy { GameCopiesIn = owned, SetsFromApi = owned };
        var api = new ClientEntityApi(_store, NullLogger<ClientEntityApi>.Instance, new Lazy<IMappingPolicyDirectory>(() => new Directory(policy)));
        var entity = _store.CreateEntity(new TelemetryComponent(), new RigComponent(), new RosterComponent());
        var handle = new EntityHandle(entity.RawEntity, api);

        NativeInitRegistry.InitAll(handle, RosterAccessors.Components);

        return Shaped<Rig>(handle);
    }

    private static bool DirtyOf(Rig rig) => EntityHandle.Of(rig).GetComponent<TelemetryComponent>().IsDirty;

    private static bool FromApiOf(Rig rig) => EntityHandle.Of(rig).GetComponent<TelemetryComponent>().ChangedFromApi;

    [Fact]
    public void A_refused_mirror_leaves_the_value_alone()
    {
        var rig = Spawn(owned: false);

        rig.Ticks = 5;

        Assert.Equal(0, rig.Ticks);
        Assert.False(DirtyOf(rig));
    }

    /// A collection changes through its own members rather than by assignment, and the rule it keeps
    /// is the same one: changing it is the game reporting what it holds.
    [Fact]
    public void A_refused_mirror_leaves_a_collection_alone()
    {
        var rig = SpawnWithRoster(owned: false);

        rig.AddMembers(5);

        Assert.Equal(0, rig.MembersCount);
        Assert.False(EntityHandle.Of(rig).GetComponent<RosterComponent>().IsDirty);
    }

    [Fact]
    public void An_allowed_mirror_changes_a_collection()
    {
        var rig = SpawnWithRoster(owned: true);

        rig.AddMembers(5);

        Assert.Equal(1, rig.MembersCount);
        Assert.True(EntityHandle.Of(rig).GetComponent<RosterComponent>().IsDirty);
    }

    /// A changed collection is dirty, which is not the same as claimed, so the game keeps reporting
    /// it. The field entry used to read the dirty mask here and would have stopped after one.
    [Fact]
    public void A_collection_the_game_owns_takes_more_than_one_mirror()
    {
        var rig = SpawnWithRoster(owned: true);

        rig.AddMembers(5);
        rig.AddMembers(6);

        Assert.Equal(2, rig.MembersCount);
    }

    /// A value the API claimed is the client's until it is shown to the game, and a mirror waits its
    /// turn. A collection reads the same mask a value does, which it did not before.
    [Fact]
    public void A_claimed_collection_refuses_a_mirror()
    {
        var rig = SpawnWithRoster(owned: true);

        rig.AddMembers(5);
        EntityHandle.Of(rig).GetComponent<RosterComponent>().MarkChangedFromApi();

        rig.AddMembers(6);

        Assert.Equal(1, rig.MembersCount);
    }

    [Fact]
    public void A_refused_override_leaves_the_value_alone()
    {
        var rig = Spawn(owned: false);

        Assert.False(rig.Override(Telemetry.Field.Ticks, 5));
        Assert.Equal(0, rig.Ticks);
        Assert.False(FromApiOf(rig));
    }

    /// Reporting what the game did: it replicates, and nothing claims the value from the game.
    [Fact]
    public void An_allowed_mirror_writes_without_claiming_the_value()
    {
        var rig = Spawn(owned: true);

        rig.Ticks = 5;

        Assert.Equal(5, rig.Ticks);
        Assert.True(DirtyOf(rig));
        Assert.False(FromApiOf(rig));
    }

    [Fact]
    public void An_allowed_override_claims_the_value()
    {
        var rig = Spawn(owned: true);

        Assert.True(rig.Override(Telemetry.Field.Ticks, 5));
        Assert.Equal(5, rig.Ticks);
        Assert.True(DirtyOf(rig));
        Assert.True(FromApiOf(rig));
    }

    /// The game must not report over a value the mod has claimed and it has not been shown yet.
    [Fact]
    public void A_claimed_value_survives_a_report_from_the_game()
    {
        var rig = Spawn(owned: true);

        rig.Override(Telemetry.Field.Ticks, 42);
        rig.Ticks = 3;

        Assert.Equal(42, rig.Ticks);
    }
}
