using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Exceptions;
using ReadyM.SDK.Server.Events;

namespace ReadyM.SDK.Tests.Server;

/// <summary>The world entity, read before the relay has created it.</summary>
public class ServerWorldTests : ServerSdkTest
{
    public ServerWorldTests()
    {
        foreach (var component in ComponentsOf<World>().Types)
            Relay.RegisterUnnamed(component);
    }

    [Fact]
    public void Reading_the_world_before_it_exists_names_the_event_from_which_it_does()
    {
        var thrown = Assert.Throws<InvalidEntityException>(() => Entities.World);

        Assert.Contains($"{nameof(IServerEvents)}.{nameof(IServerEvents.OnWorldCreated)}", thrown.Message);
        Assert.Contains("Start", thrown.Message);
    }
}
