using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Server;
using ReadyM.SDK.Tests.Server.Fixtures;
using CoreArea = ReadyM.SDK.Tests.Client.Fixtures.CoreArea;
using LedgeredComponent = ReadyM.SDK.Tests.Client.Fixtures.LedgeredComponent;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Tests.Server;

// An owner streaming a value every frame would overwrite whatever the server set, unless the server's write stamps
// the field with its time. Deltas the owner sent before it saw that time are then refused, which is what lets a
// server write win against a value the owner keeps sending
public class ServerChangeTimeTests : ServerSdkTest
{
    private const uint ServerTime = 42;

    // Stands in for the relay's change components, keyed the way the generated code asks: by entity id
    private sealed class RecordingChangeStore : IChangeTrackingStore
    {
        private sealed class Box<T>
        {
            public T Value = default!;
        }

        private readonly Dictionary<(int Id, Type Change), object> _boxes = [];

        public ref T GetChangeComponent<T>(int id) where T : struct, IComponent
        {
            if (!_boxes.TryGetValue((id, typeof(T)), out var box))
                _boxes[(id, typeof(T))] = box = new Box<T>();

            return ref ((Box<T>)box).Value;
        }

        public T Read<T>(int id) where T : struct, IComponent
            => _boxes.TryGetValue((id, typeof(T)), out var box) ? ((Box<T>)box).Value : default;
    }

    private readonly RecordingChangeStore _changes = new();

    private uint StampOf(Probe probe)
        => _changes.Read<TelemetryComponent.ChangeComponent>(IdentityOf(probe).Id).TicksLastChanged;

    [Fact]
    public void A_write_through_a_handle_stamps_the_field_with_the_server_time()
    {
        var probe = Entities.Create<Probe>();

        using (ComponentWriteContext.EnterServerAuthoring(ServerTime, _changes))
            probe.Ticks = 5;

        Assert.Equal(5, Relay.Get<TelemetryComponent>(IdentityOf(probe)).Ticks);
        Assert.Equal(ServerTime, StampOf(probe));
    }

    [Fact]
    public void A_write_through_a_query_stamps_the_field_with_the_server_time()
    {
        var probe = Entities.Create<Probe>();

        using (ComponentWriteContext.EnterServerAuthoring(ServerTime, _changes))
        {
            foreach (var found in Entities.Query<Probe>())
                found.Ticks = 9;
        }

        Assert.Equal(9, Relay.Get<TelemetryComponent>(IdentityOf(probe)).Ticks);
        Assert.Equal(ServerTime, StampOf(probe));
    }

    [Fact]
    public void A_server_write_is_sent_to_the_owner_as_an_override()
    {
        var probe = Entities.Create<Probe>();

        using (ComponentWriteContext.EnterServerAuthoring(ServerTime, _changes))
            probe.Ticks = 5;

        Assert.True(Relay.Get<TelemetryComponent>(IdentityOf(probe)).ChangedFromApi);
    }

    // The server saying the same thing again still has to win against what the owner sent in between
    [Fact]
    public void Writing_the_same_value_again_moves_the_stamp_on()
    {
        var probe = Entities.Create<Probe>();

        using (ComponentWriteContext.EnterServerAuthoring(ServerTime, _changes))
            probe.Ticks = 5;

        using (ComponentWriteContext.EnterServerAuthoring(ServerTime + 8, _changes))
            probe.Ticks = 5;

        Assert.Equal(ServerTime + 8, StampOf(probe));
    }

    // Outside a mod tick or an RPC handler there is no time to stamp with, and the write still lands
    [Fact]
    public void A_write_outside_server_authoring_needs_no_change_store()
    {
        var probe = Entities.Create<Probe>();

        probe.Ticks = 3;

        Assert.Equal(3, Relay.Get<TelemetryComponent>(IdentityOf(probe)).Ticks);
    }

    [Fact]
    public void A_local_archetype_carries_the_change_component_of_a_replicated_mixin_it_includes()
        => Assert.True(EntityHandle.Of(Entities.Create<Probe>()).HasComponent<TelemetryComponent.ChangeComponent>());

    // The relay adds the change components of the game's own components to its archetypes, but a mod's component
    // is only a struct index to it, so the extension has to bring its change component along
    [Fact]
    public void An_extension_brings_the_change_component_of_a_replicated_mixin()
    {
        var contributed = ServerArchetypes.Contributed(typeof(CoreArea));

        Assert.Contains(typeof(LedgeredComponent), contributed);
        Assert.Contains(typeof(LedgeredComponent.ChangeComponent), contributed);
    }

    [Fact]
    public void An_extension_brings_no_change_component_for_a_local_mixin()
    {
        var contributed = ServerArchetypes.Contributed(typeof(CoreArea));

        Assert.DoesNotContain(contributed, component => component.DeclaringType == typeof(ReadyM.SDK.Tests.Client.Fixtures.WeatherComponent));
    }
}
