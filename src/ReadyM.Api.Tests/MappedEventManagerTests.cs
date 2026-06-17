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

    private NativeMappedEventManager GetManager()
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
        }, default(EmptyContext));

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
        }, default(EmptyContext));

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
        }, default(EmptyContext));

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
        }, default(EmptyContext));

        // Assert
        Assert.True(ecsHandled);
    }
}