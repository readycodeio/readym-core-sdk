using System.Runtime.InteropServices;
using DryIoc;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.Api.Mapping.Policies.Event.Common;
using ReadyM.Api.Mapping.Tags;
using ReadyM.Api.Tests.TestEvents;

namespace ReadyM.Api.Tests;

public class MappedEventManagerTests
{
    private class TestEventsRegistration : INativeComponentRegistration
    {
        public void Register(INativeComponentRegistry registry)
        {
            registry.RegisterComponent<NativeEvent>();
        }
    }

    /// <param name="ecsThread">The thread the ECS runs on; with none yet, an ECS handler runs at once.</param>
    private (NativeMappedEventManager Manager, PendingActionUpdater<CommandBufferSynced> EcsScheduler) GetManager(Thread? ecsThread)
    {
        var container = new Container(rules =>
            rules.With(FactoryMethod.ConstructorWithResolvableArguments)
                .WithDefaultReuse(Reuse.Singleton)
                .WithUseInterpretation()
        );

        container.Register<DataSideChannel>();
        container.Register<IMappingEventPolicyFactory, AlwaysPropagatesEventPolicyFactory>();

        container.RegisterMany<NativeMappingPolicyDirectory>(serviceTypeCondition: type => type.IsInterface, nonPublicServiceTypes: true);
        container.RegisterInitializer<IMappingPolicyDirectory>((iface, s) =>
        {
            var mapping = (MappingPolicyDirectory)iface;

            foreach (var factory in s.ResolveMany<IMappingDataPolicyFactory>())
            {
                mapping.RegisterDefaultData(factory);
            }

            foreach (var factory in s.ResolveMany<IMappingEventPolicyFactory>())
            {
                mapping.RegisterDefaultEvent(factory);
            }
        });


        var loggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
        container.RegisterInstance(loggerFactory);
        container.Register(typeof(ILogger<>), typeof(Logger<>), ifAlreadyRegistered: IfAlreadyRegistered.Replace);
        container.RegisterInstance(loggerFactory.CreateLogger("Tests"));

        container.Register<INativeComponentRegistration, TestEventsRegistration>();

        container.Register<INativeComponentRegistry, NativeComponentRegistry>();
        container.Register<NativeMappedEventManager>();

        container.RegisterInstance(new EntityStore());
        container.RegisterMany<Store>(nonPublicServiceTypes: true);

        container.Register<IMappedEntityManager<IntPtr>, MappedEntityManager<IntPtr>>();

        var ecsScheduler = new PendingActionUpdater<CommandBufferSynced>(null!, container.Resolve<ILogger>());
        ecsScheduler.SetThread(ecsThread);
        container.RegisterInstance<PendingActionScheduler<CommandBufferSynced>>(ecsScheduler);

        return (container.Resolve<NativeMappedEventManager>(), ecsScheduler);
    }

    /// <summary>Runs on a thread of its own and waits for it, which a task does not promise: a waited task may run inline.</summary>
    private static void OnAnotherThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.Start();
        thread.Join();

        if (failure is not null)
            throw new InvalidOperationException("The other thread failed", failure);
    }

    [Fact]
    public void ReactsToEcsEvents()
    {
        // Arrange
        var (manager, _) = GetManager(ecsThread: null);
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
        }, default(EmptyContext));

        // Assert
        Assert.True(ecsHandled);
    }

    [Fact]
    public void ReactsToGameEvents()
    {
        // Arrange
        var (manager, _) = GetManager(ecsThread: null);
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
        }, default(EmptyContext));

        // Assert
        Assert.True(handled);
    }

    [Fact]
    public void ReactsToBothEvents()
    {
        // Arrange
        var (manager, _) = GetManager(ecsThread: null);
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
        }, default(EmptyContext));

        // Assert
        Assert.True(ecsHandled);
        Assert.True(gameHandled);
    }

    [Fact]
    public void ReactsToNativeEcsEvents()
    {
        // Arrange
        var (manager, _) = GetManager(ecsThread: null);
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
        var (manager, _) = GetManager(ecsThread: null);
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
        }, default(EmptyContext));

        // Assert
        Assert.True(ecsHandled);
    }

    /// <summary>The game reports most events on its own threads, and a handler must only ever see the ECS thread.</summary>
    [Fact]
    public void An_ecs_handler_notified_from_another_thread_runs_on_the_ecs_threads_next_update()
    {
        var (manager, ecsScheduler) = GetManager(Thread.CurrentThread);
        Thread? ranOn = null;

        manager.RegisterEcsEventHandler<ManagedEvent>(ev =>
        {
            Assert.Equal(5, ev.IntValue);
            ranOn = Thread.CurrentThread;
        });

        var propagated = false;
        OnAnotherThread(() => propagated = manager.NotifyEcsIfApplicable(new ManagedEvent { IntValue = 5 }, default(EmptyContext)));

        Assert.True(propagated);
        Assert.Null(ranOn);

        ecsScheduler.Update();

        Assert.Same(Thread.CurrentThread, ranOn);
    }

    /// <summary>Notified on the ECS thread itself, the handler runs before the notify returns.</summary>
    [Fact]
    public void An_ecs_handler_notified_on_the_ecs_thread_runs_at_once()
    {
        var (manager, _) = GetManager(Thread.CurrentThread);
        var handled = false;

        manager.RegisterEcsEventHandler<ManagedEvent>(_ => handled = true);
        manager.NotifyEcsIfApplicable(new ManagedEvent { IntValue = 5 }, default(EmptyContext));

        Assert.True(handled);
    }

    /// <summary>
    /// A handler that runs later still counts as the event propagating to the ECS, so invoking the same event in the
    /// game from it is refused rather than looping back.
    /// </summary>
    [Fact]
    public void A_deferred_ecs_handler_cannot_invoke_its_event_back_into_the_game()
    {
        var (manager, ecsScheduler) = GetManager(Thread.CurrentThread);
        bool? invokedInGame = null;

        manager.RegisterGameEventHandler<ManagedEvent>(_ => Assert.Fail("The event looped back into the game."));
        manager.RegisterEcsEventHandler<ManagedEvent>(ev =>
            invokedInGame = manager.InvokeInGameIfApplicable(ev, default(EmptyContext)));

        OnAnotherThread(() => manager.NotifyEcsIfApplicable(new ManagedEvent { IntValue = 5 }, default(EmptyContext)));
        ecsScheduler.Update();

        Assert.False(invokedInGame);
    }

    /// <summary>A native event is read while the game's copy exists, and handled on the ECS thread afterwards.</summary>
    [Fact]
    public void A_native_ecs_event_is_read_at_once_and_handled_on_the_ecs_thread()
    {
        var (manager, ecsScheduler) = GetManager(Thread.CurrentThread);
        int? handledValue = null;

        manager.RegisterEcsEventHandler<NativeEvent>(ev => handledValue = ev.IntValue);

        OnAnotherThread(() =>
        {
            var evPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeEvent>());
            try
            {
                Marshal.StructureToPtr(new NativeEvent { Actor = IntPtr.Zero, IntValue = 7 }, evPtr, false);
                manager.NotifyEcsIfApplicable(NativeEvent.Id, evPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(evPtr);
            }
        });

        Assert.Null(handledValue);

        ecsScheduler.Update();

        Assert.Equal(7, handledValue);
    }
}
