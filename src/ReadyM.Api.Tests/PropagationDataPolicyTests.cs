using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.Policies.Data.Common;
using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Tests;

/// The three policies a component carries when it says which way its values travel and nothing
/// about who owns it. Pull is game to ECS, push is ECS to game, override is a write from the API.
public class PropagationDataPolicyTests
{
    private struct BothWays : IAlwaysPropagates;

    private struct GameOwned : IAlwaysPropagatesToEcsOnly;

    private struct EcsOwned : IAlwaysPropagatesToGameOnly;

    private struct ServerOwned : IServerAuthoritative;

    private readonly DataSideChannel _channel = new();
    private readonly MappingPolicyDirectory _directory;

    public PropagationDataPolicyTests()
    {
        _directory = new MappingPolicyDirectory(_channel);
        _directory.RegisterDefaultData(new PropagationDataPolicyFactory(_channel));
    }

    [Fact]
    public void Both_ways_allows_everything()
    {
        var policy = _directory.ForData(typeof(BothWays));

        Assert.True(policy.ShouldGameCopyToEcs(default));
        Assert.True(policy.ShouldEcsCopyToGame(default));
        Assert.True(policy.CanSetFromApi(default));
        Assert.True(policy.CanGameSetLocally(default));
    }

    [Fact]
    public void A_game_owned_value_is_pulled_and_never_written()
    {
        var policy = _directory.ForData(typeof(GameOwned));

        Assert.True(policy.ShouldGameCopyToEcs(default));
        Assert.False(policy.ShouldEcsCopyToGame(default));
        Assert.False(policy.CanSetFromApi(default));
        Assert.True(policy.CanGameSetLocally(default));
    }

    [Fact]
    public void An_ecs_owned_value_is_pushed_and_never_read_back()
    {
        var policy = _directory.ForData(typeof(EcsOwned));

        Assert.False(policy.ShouldGameCopyToEcs(default));
        Assert.True(policy.ShouldEcsCopyToGame(default));
        Assert.True(policy.CanSetFromApi(default));
        Assert.False(policy.CanGameSetLocally(default));
    }

    /// The server's word is applied to the game and nothing on this side writes it back.
    [Fact]
    public void A_server_owned_value_is_pushed_and_never_written_here()
    {
        var policy = _directory.ForData(typeof(ServerOwned));

        Assert.False(policy.ShouldGameCopyToEcs(default));
        Assert.True(policy.ShouldEcsCopyToGame(default));
        Assert.False(policy.CanSetFromApi(default));
        Assert.False(policy.CanGameSetLocally(default));
    }

    /// Applying a value to the game must not read it straight back out, and the reverse.
    [Fact]
    public void A_propagation_in_flight_does_not_bounce()
    {
        var policy = _directory.ForData(typeof(BothWays));

        using (_channel.PushScope<PropagatingToGameScope<BothWays>>())
            Assert.False(policy.ShouldGameCopyToEcs(default));

        using (_channel.PushScope<PropagatingToEcsScope<BothWays>>())
            Assert.False(policy.ShouldEcsCopyToGame(default));
    }

    /// A component that says nothing still has no policy, which is what the caller reads as "no rule".
    [Fact]
    public void A_component_that_says_nothing_is_refused()
    {
        Assert.Throws<ArgumentException>(() => _directory.ForData(typeof(Entity)));
    }
}
