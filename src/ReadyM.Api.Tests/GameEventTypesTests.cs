using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Tests.TestEvents;

namespace ReadyM.Api.Tests;

public class GameEventTypesTests
{
    [Fact]
    public void GameEventResultHasTheByteValuesTheNativeSideMirrors()
    {
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(GameEventResult)));
        Assert.Equal(0x00, (byte)GameEventResult.DontRun);
        Assert.Equal(0x01, (byte)GameEventResult.RunOptimistic);
        Assert.Equal(0x02, (byte)GameEventResult.RunAuthoritative);
        Assert.Equal(0x04, (byte)GameEventResult.UndoOptimistic);
        Assert.Equal(0x80, (byte)GameEventResult.Rejected);
        Assert.Equal(0x03, (byte)GameEventResult.RunAll);
        Assert.Equal(6, Enum.GetValues<GameEventResult>().Length);
    }

    [Fact]
    public void GameEventNotifyResultHasTwoValues()
    {
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(GameEventNotifyResult)));
        Assert.Equal(0, (byte)GameEventNotifyResult.DontNotify);
        Assert.Equal(1, (byte)GameEventNotifyResult.Notify);
        Assert.Equal(2, Enum.GetValues<GameEventNotifyResult>().Length);
    }

    [Theory]
    [InlineData(GameEventResult.DontRun, false, false)]
    [InlineData(GameEventResult.RunOptimistic, true, false)]
    [InlineData(GameEventResult.RunAuthoritative, true, false)]
    [InlineData(GameEventResult.RunAll, true, false)]
    [InlineData(GameEventResult.UndoOptimistic, false, false)]
    [InlineData(GameEventResult.Rejected, false, true)]
    public void RunsAndIsRejectedReadTheFlags(GameEventResult result, bool runs, bool rejected)
    {
        Assert.Equal(runs, result.Runs());
        Assert.Equal(rejected, result.IsRejected());
    }

    private sealed class Registration(Action<IAllTypeRegistry> register) : IAllTypeRegistration
    {
        public void Register(IAllTypeRegistry registry) => register(registry);
    }

    private sealed class Source(params object[] contexts) : IGameEventContextSource
    {
        public List<Type> Asked { get; } = [];

        public T Get<T>() where T : class
        {
            Asked.Add(typeof(T));
            return contexts.OfType<T>().FirstOrDefault()
                   ?? throw new InvalidOperationException($"No {typeof(T).Name} in the test source");
        }
    }

    private static GameEventContextRegistry Build(Source source, Action<IAllTypeRegistry> register)
        => new(new AllTypeRegistry([new Registration(register)]), source);

    [Fact]
    public void ContextRegistryHoldsWhatTheEventsRequireEachGotOnce()
    {
        var first = new FirstContext(7);
        var second = new SecondContext("second");
        var source = new Source(first, second);

        var registry = Build(source, r =>
        {
            r.RegisterEvent<NeedsFirstEvent>();
            r.RegisterEvent<NeedsBothEvent>();
        });

        Assert.Same(first, registry.GetContext<FirstContext>());
        Assert.Same(second, registry.GetContext<SecondContext>());
        Assert.Equal([typeof(FirstContext), typeof(SecondContext)], source.Asked);
    }

    [Fact]
    public void ContextRegistryIgnoresComponentsAndEventsThatRequireNothing()
    {
        var source = new Source();

        var registry = Build(source, r => r.RegisterEvent<ManagedEvent>());

        Assert.Empty(source.Asked);
        var error = Assert.Throws<InvalidOperationException>(() => registry.GetContext<FirstContext>());
        Assert.Contains(nameof(FirstContext), error.Message);
    }

    [Fact]
    public void ContextRegistryFailsNamingTheEventWhenATypeCannotBeProvided()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Build(new Source(new FirstContext(1)), r =>
        {
            r.RegisterEvent<NeedsFirstEvent>();
            r.RegisterEvent<NeedsBothEvent>();
        }));

        Assert.Contains(nameof(NeedsBothEvent), error.Message);
        Assert.Contains(nameof(SecondContext), error.Message);
    }

    [Fact]
    public void ContextRegistriesAreIndependent()
    {
        var one = Build(new Source(new FirstContext(1)), r => r.RegisterEvent<NeedsFirstEvent>());
        var two = Build(new Source(new FirstContext(2)), r => r.RegisterEvent<NeedsFirstEvent>());

        Assert.Equal(1, one.GetContext<FirstContext>().Value);
        Assert.Equal(2, two.GetContext<FirstContext>().Value);
    }

    [Fact]
    public void GettingAContextDoesNotAllocate()
    {
        var registry = Build(new Source(new FirstContext(3)), r => r.RegisterEvent<NeedsFirstEvent>());
        var sum = registry.GetContext<FirstContext>().Value;

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            sum += registry.GetContext<FirstContext>().Value;
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
        Assert.Equal(3 * 1001, sum);
    }
}

internal sealed class FirstContext(int value)
{
    public int Value { get; } = value;
}

internal sealed class SecondContext(string name)
{
    public string Name { get; } = name;
}

/// Hand-written events naming what they read; the generator writes their AcceptContexts.
internal partial struct NeedsFirstEvent : IGameEvent, IGameEventRequiresContext<FirstContext>
{
    public GameEventNotifyResult CanGameEventNotifyEcs(GameEventContextRegistry contexts) => GameEventNotifyResult.Notify;
    public GameEventResult CanGameEventRunLocally(GameEventContextRegistry contexts) => GameEventResult.RunAll;
    public GameEventResult CanEcsInvokeGameEvent(GameEventContextRegistry contexts) => GameEventResult.RunAll;
}

internal partial struct NeedsBothEvent : IGameEvent, IGameEventRequiresContext<FirstContext>, IGameEventRequiresContext<SecondContext>
{
    public GameEventNotifyResult CanGameEventNotifyEcs(GameEventContextRegistry contexts) => GameEventNotifyResult.Notify;
    public GameEventResult CanGameEventRunLocally(GameEventContextRegistry contexts) => GameEventResult.RunAll;
    public GameEventResult CanEcsInvokeGameEvent(GameEventContextRegistry contexts) => GameEventResult.RunAll;
}
