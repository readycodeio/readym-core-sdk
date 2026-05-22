using System.Runtime.InteropServices;
using ReadyM.Api.Helpers;
using ReadyM.Api.Interop.Registry;
using ReadyM.Api.Mapping;

namespace ReadyM.Api.Tests;

[InteropType]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct NativeEvent
{
    public int IntValue { get; init; }
    public float FloatValue { get; init; }
}

public class DataSideChannelTests
{
    private struct ManagedEvent
    {
        public int IntValue { get; init; }
        public float FloatValue { get; init; }
    }

    private struct AnotherManagedEvent
    {
        public int IntValue { get; init; }
        public float FloatValue { get; init; }
    }

    [Fact]
    public void HandlesPushingManagedEcsEventScope()
    {
        var channel = new DataSideChannel();
        using (channel.PushScope<PropagatingToEcsScope<ManagedEvent>>())
        {
            Assert.True(channel.HasData<PropagatingToEcsScope<ManagedEvent>>());
            Assert.False(channel.HasData<PropagatingToGameScope<ManagedEvent>>());
        }

        Assert.False(channel.HasData<PropagatingToEcsScope<ManagedEvent>>());
        Assert.False(channel.HasData<PropagatingToGameScope<ManagedEvent>>());
    }

    [Fact]
    public void HandlesPushingManagedGameEventScope()
    {
        var channel = new DataSideChannel();
        using (channel.PushScope<PropagatingToGameScope<ManagedEvent>>())
        {
            Assert.True(channel.HasData<PropagatingToGameScope<ManagedEvent>>());
            Assert.False(channel.HasData<PropagatingToEcsScope<ManagedEvent>>());
        }

        Assert.False(channel.HasData<PropagatingToGameScope<ManagedEvent>>());
        Assert.False(channel.HasData<PropagatingToEcsScope<ManagedEvent>>());
    }

    [Fact]
    public void HandlesPushingNestedManagedEventScopes()
    {
        var channel = new DataSideChannel();

        Assert.False(channel.HasData<PropagatingToEcsScope<ManagedEvent>>());
        Assert.False(channel.HasData<PropagatingToGameScope<ManagedEvent>>());
        Assert.False(channel.HasData<PropagatingToEcsScope<AnotherManagedEvent>>());
        Assert.False(channel.HasData<PropagatingToGameScope<AnotherManagedEvent>>());

        using (channel.PushScope<PropagatingToEcsScope<ManagedEvent>>())
        {
            Assert.True(channel.HasData<PropagatingToEcsScope<ManagedEvent>>());
            Assert.False(channel.HasData<PropagatingToGameScope<ManagedEvent>>());
            Assert.False(channel.HasData<PropagatingToEcsScope<AnotherManagedEvent>>());
            Assert.False(channel.HasData<PropagatingToGameScope<AnotherManagedEvent>>());

            using (channel.PushScope<PropagatingToGameScope<AnotherManagedEvent>>())
            {
                Assert.True(channel.HasData<PropagatingToEcsScope<ManagedEvent>>());
                Assert.False(channel.HasData<PropagatingToGameScope<ManagedEvent>>());
                Assert.False(channel.HasData<PropagatingToEcsScope<AnotherManagedEvent>>());
                Assert.True(channel.HasData<PropagatingToGameScope<AnotherManagedEvent>>());
            }

            Assert.True(channel.HasData<PropagatingToEcsScope<ManagedEvent>>());
            Assert.False(channel.HasData<PropagatingToGameScope<ManagedEvent>>());
            Assert.False(channel.HasData<PropagatingToEcsScope<AnotherManagedEvent>>());
            Assert.False(channel.HasData<PropagatingToGameScope<AnotherManagedEvent>>());
        }

        Assert.False(channel.HasData<PropagatingToEcsScope<ManagedEvent>>());
        Assert.False(channel.HasData<PropagatingToGameScope<ManagedEvent>>());
        Assert.False(channel.HasData<PropagatingToEcsScope<AnotherManagedEvent>>());
        Assert.False(channel.HasData<PropagatingToGameScope<AnotherManagedEvent>>());
    }

    [Fact]
    public void HandlesPushingNativeScopes()
    {
        var channel = new DataSideChannel();

        using (channel.PushScope(PropagationDirection.ToEcs, NativeEvent.Id))
        {
            Assert.True(channel.HasData(PropagationDirection.ToEcs, NativeEvent.Id));
            Assert.True(channel.HasData<PropagatingToEcsScope<NativeEvent>>());
        }

        Assert.False(channel.HasData(PropagationDirection.ToEcs, NativeEvent.Id));
        Assert.False(channel.HasData<PropagatingToEcsScope<NativeEvent>>());
    }

    [Fact]
    public void HandlesPushingNativeScopesAsManaged()
    {
        var channel = new DataSideChannel();

        using (channel.PushScope<PropagatingToGameScope<NativeEvent>>())
        {
            Assert.True(channel.HasData(PropagationDirection.ToGame, NativeEvent.Id));
            Assert.True(channel.HasData<PropagatingToGameScope<NativeEvent>>());
        }

        Assert.False(channel.HasData(PropagationDirection.ToGame, NativeEvent.Id));
        Assert.False(channel.HasData<PropagatingToGameScope<NativeEvent>>());
    }
}