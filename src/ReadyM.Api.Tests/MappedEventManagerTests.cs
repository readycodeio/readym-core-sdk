using System.Runtime.InteropServices;
using DryIoc;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Tests.TestEvents;

namespace ReadyM.Api.Tests;

public class MappedEventManagerTests
{
    private class TestEventsRegistration : INativeTypeRegistration
    {
        public void Register(INativeTypeRegistry registry)
        {
            registry.RegisterEvent<NativeEvent>();
        }
    }

    /// A hand-written policy whose answers a test sets.
    private struct ProbeEvent : IGameEvent
    {
        public int Value;
        public GameEventNotifyResult Notify;
        public GameEventResult RunLocally;
        public GameEventResult Invoke;

        public GameEventNotifyResult CanGameEventNotifyEcs(GameEventContextRegistry contexts) => Notify;
        public GameEventResult CanGameEventRunLocally(GameEventContextRegistry contexts) => RunLocally;
        public GameEventResult CanEcsInvokeGameEvent(GameEventContextRegistry contexts) => Invoke;
    }

    private static NativeMappedEventManager GetManager()
    {
        var container = new Container(rules =>
            rules.With(FactoryMethod.ConstructorWithResolvableArguments)
                .WithDefaultReuse(Reuse.Singleton)
                .WithUseInterpretation()
        );

        container.Register<DataSideChannel>();
        container.RegisterInstance(new GameEventContextRegistry(new AllTypeRegistry([]), new ResolverGameEventContextSource(container)));

        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new FailOnErrorLoggerProvider()));
        container.RegisterInstance(loggerFactory);
        container.Register(typeof(ILogger<>), typeof(Logger<>), ifAlreadyRegistered: IfAlreadyRegistered.Replace);
        container.RegisterInstance(loggerFactory.CreateLogger("Tests"));

        container.Register<INativeTypeRegistration, TestEventsRegistration>();
        container.Register<INativeTypeRegistry, NativeTypeRegistry>();
        container.Register<NativeMappedEventManager>();

        container.RegisterInstance(new EntityStore());
        container.RegisterMany<Store>(nonPublicServiceTypes: true);

        return container.Resolve<NativeMappedEventManager>();
    }

    [Fact]
    public void ReactsToEcsEvents()
    {
        // Arrange
        var manager = GetManager();
        var ecsHandled = false;

        manager.RegisterEcsEventHandler<ManagedEvent>(ev =>
        {
            Assert.Equal(5, ev.IntValue);
            ecsHandled = true;
        });

        manager.RegisterGameEventHandler<ManagedEvent>(ev => { Assert.Fail(); });

        // Act
        manager.NotifyEcsIfApplicable(new ManagedEvent
        {
            IntValue = 5,
            FloatValue = 0.0f
        });

        // Assert
        Assert.True(ecsHandled);
    }

    [Fact]
    public void ReactsToGameEvents()
    {
        // Arrange
        var manager = GetManager();
        var handled = false;

        manager.RegisterGameEventHandler<ManagedEvent>(ev =>
        {
            Assert.Equal(5, ev.IntValue);
            handled = true;
        });

        manager.RegisterEcsEventHandler<ManagedEvent>(ev => { Assert.Fail(); });

        // Act
        manager.InvokeInGameIfApplicable(new ManagedEvent
        {
            IntValue = 5,
            FloatValue = 0.0f
        });

        // Assert
        Assert.True(handled);
    }

    [Fact]
    public void ReactsToBothEvents()
    {
        // Arrange
        var manager = GetManager();
        var ecsHandled = false;
        var gameHandled = false;

        manager.RegisterEcsEventHandler<ManagedEvent>(ev =>
        {
            Assert.Equal(5, ev.IntValue);
            ecsHandled = true;
        });

        manager.RegisterGameEventHandler<ManagedEvent>(ev =>
        {
            Assert.Equal(5, ev.IntValue);
            gameHandled = true;
        });

        // Act
        manager.InvokeInGameAndNotifyEcs(new ManagedEvent
        {
            IntValue = 5,
            FloatValue = 0.0f
        });

        // Assert
        Assert.True(ecsHandled);
        Assert.True(gameHandled);
    }

    [Fact]
    public void ReactsToNativeEcsEvents()
    {
        // Arrange
        var manager = GetManager();
        var ecsHandled = false;

        manager.RegisterEcsEventHandler<NativeEvent>(ev =>
        {
            Assert.Equal(IntPtr.Zero, ev.Actor);
            Assert.Equal(5, ev.IntValue);
            ecsHandled = true;
        });

        manager.RegisterGameEventHandler<NativeEvent>(ev => { Assert.Fail(); });

        var ev = new NativeEvent
        {
            Actor = IntPtr.Zero,
            IntValue = 5,
        };

        var evPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeEvent>());
        try
        {
            Marshal.StructureToPtr(ev, evPtr, false);
            manager.NotifyEcsIfApplicable(NativeEvent.Id, evPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(evPtr);
        }

        // Assert
        Assert.True(ecsHandled);
    }

    [Fact]
    public void ReactsToNativeEcsEventsAsManaged()
    {
        // Arrange
        var manager = GetManager();
        var ecsHandled = false;

        manager.RegisterEcsEventHandler<NativeEvent>(ev =>
        {
            Assert.Equal(5, ev.IntValue);
            ecsHandled = true;
        });

        manager.RegisterGameEventHandler<NativeEvent>(ev => { Assert.Fail(); });

        // Act
        manager.NotifyEcsIfApplicable(new NativeEvent
        {
            Actor = IntPtr.Zero,
            IntValue = 5,
        });

        // Assert
        Assert.True(ecsHandled);
    }

    [Fact]
    public void NativeCanRunLocallyAsksWithoutNotifying()
    {
        var manager = GetManager();
        var ecsCalls = 0;
        manager.RegisterEcsEventHandler<NativeEvent, object?>((_, _) => ecsCalls++, null);

        var evPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeEvent>());
        try
        {
            Marshal.StructureToPtr(new NativeEvent { Actor = IntPtr.Zero, IntValue = 5 }, evPtr, false);
            Assert.Equal((byte)GameEventResult.RunAll, manager.CanGameEventRunLocally(NativeEvent.Id, evPtr));
        }
        finally
        {
            Marshal.FreeHGlobal(evPtr);
        }

        Assert.Equal(0, ecsCalls);
    }

    [Fact]
    public void AnUnknownNativeEventIdIsAnError()
    {
        var manager = GetManager();

        Assert.ThrowsAny<Exception>(() => manager.NotifyEcsIfApplicable(NativeEvent.Id + 100, IntPtr.Zero));
        Assert.ThrowsAny<Exception>(() => manager.InvokeInGameIfApplicable(NativeEvent.Id + 100, IntPtr.Zero));
        Assert.ThrowsAny<Exception>(() => manager.CanGameEventRunLocally(NativeEvent.Id + 100, IntPtr.Zero));
    }

    [Theory]
    [InlineData(GameEventNotifyResult.Notify, true)]
    [InlineData(GameEventNotifyResult.DontNotify, false)]
    public void NotifyAsksTheEventAndSaysWhetherItNotified(GameEventNotifyResult notify, bool notified)
    {
        var manager = GetManager();
        var ecsCalls = 0;
        manager.RegisterEcsEventHandler<ProbeEvent, object?>((_, _) => ecsCalls++, null);

        var result = manager.NotifyEcsIfApplicable(new ProbeEvent { Notify = notify, RunLocally = GameEventResult.Rejected });

        Assert.Equal(notified ? 1 : 0, ecsCalls);
        Assert.Equal(notified, result);
    }

    [Theory]
    [InlineData(GameEventResult.RunAll, 1)]
    [InlineData(GameEventResult.RunOptimistic, 1)]
    [InlineData(GameEventResult.DontRun, 0)]
    [InlineData(GameEventResult.Rejected, 0)]
    public void InvokeRunsTheGameHandlersExactlyWhenTheEventSaysItRuns(GameEventResult invoke, int expectedCalls)
    {
        var manager = GetManager();
        var gameCalls = 0;
        manager.RegisterGameEventHandler<ProbeEvent, object?>((_, _) => gameCalls++, null);

        var result = manager.InvokeInGameIfApplicable(new ProbeEvent { Invoke = invoke });

        Assert.Equal(expectedCalls, gameCalls);
        Assert.Equal(expectedCalls == 1, result);
    }

    [Fact]
    public void WhilePropagatingToTheGameTheEventIsNotSentBackAndRunsLocally()
    {
        var manager = GetManager();
        var echo = new ProbeEvent { Notify = GameEventNotifyResult.Notify, RunLocally = GameEventResult.Rejected, Invoke = GameEventResult.RunAll };
        var ecsCalls = 0;
        bool? notifyResult = null;
        GameEventNotifyResult? canNotify = null;
        GameEventResult? canRun = null;

        manager.RegisterEcsEventHandler<ProbeEvent, object?>((_, _) => ecsCalls++, null);
        manager.RegisterGameEventHandler<ProbeEvent, object?>((_, _) =>
        {
            notifyResult = manager.NotifyEcsIfApplicable(echo);
            canNotify = manager.CanGameEventNotifyEcs(echo);
            canRun = manager.CanGameEventRunLocally(echo);
        }, null);

        manager.InvokeInGameIfApplicable(echo);

        Assert.Equal(0, ecsCalls);
        Assert.False(notifyResult);
        Assert.Equal(GameEventNotifyResult.DontNotify, canNotify);
        Assert.Equal(GameEventResult.RunAll, canRun);

        Assert.Equal(GameEventNotifyResult.Notify, manager.CanGameEventNotifyEcs(echo));
        Assert.Equal(GameEventResult.Rejected, manager.CanGameEventRunLocally(echo));
    }

    [Fact]
    public void WhilePropagatingToTheEcsTheEventIsNotPlayedBack()
    {
        var manager = GetManager();
        var echo = new ProbeEvent { Notify = GameEventNotifyResult.Notify, RunLocally = GameEventResult.RunAll, Invoke = GameEventResult.RunAll };
        var gameCalls = 0;
        bool? invokeResult = null;
        GameEventResult? canInvoke = null;

        manager.RegisterGameEventHandler<ProbeEvent, object?>((_, _) => gameCalls++, null);
        manager.RegisterEcsEventHandler<ProbeEvent, object?>((_, _) =>
        {
            invokeResult = manager.InvokeInGameIfApplicable(echo);
            canInvoke = manager.CanEcsInvokeGameEvent(echo);
        }, null);

        manager.NotifyEcsIfApplicable(echo);

        Assert.Equal(0, gameCalls);
        Assert.False(invokeResult);
        Assert.Equal(GameEventResult.DontRun, canInvoke);
        Assert.Equal(GameEventResult.RunAll, manager.CanEcsInvokeGameEvent(echo));
    }

    [Fact]
    public void NotifyingDoesNotAllocate()
    {
        var manager = GetManager();
        var sum = 0;
        manager.RegisterEcsEventHandler<ProbeEvent, object?>(static (ev, _) => { }, null);
        var ev = new ProbeEvent { Value = 1, Notify = GameEventNotifyResult.Notify, RunLocally = GameEventResult.RunAll };
        manager.NotifyEcsIfApplicable(ev);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            sum += manager.NotifyEcsIfApplicable(ev) ? 1 : 0;
        }
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
        Assert.Equal(1000, sum);
    }
}
