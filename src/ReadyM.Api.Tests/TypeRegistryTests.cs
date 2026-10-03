using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Interop.Registry;
using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.Tests;

[InteropType, DeriveIGameEvent, AlwaysPropagates]
[StructLayout(LayoutKind.Sequential)]
public partial struct FirstEvent
{
    public int Value;
}

[InteropType, DeriveIGameEvent, AlwaysPropagates]
[StructLayout(LayoutKind.Sequential)]
public partial struct SecondEvent
{
    public int Value;
}

public class TypeRegistryTests
{
    private struct FirstComponent
    {
        public static int Id;
        public int Value;
    }

    private struct SecondComponent
    {
        public static int Id;
        public int Value;
    }

    private sealed class Registration(Action<INativeTypeRegistry> register) : INativeTypeRegistration
    {
        public void Register(INativeTypeRegistry registry) => register(registry);
    }

    private sealed class RecordingCallback : ITypeRegistryCallbackBase<INativeTypeRegistry, ValueType, IGameEvent>
    {
        public List<string> Visits { get; } = [];

        public void AcceptComponent<T>(INativeTypeRegistry registry, T defaultValue = default) where T : struct
            => Visits.Add($"component {typeof(T).Name}");

        public void AcceptModComponent(INativeTypeRegistry registry, ModComponentInfo info, string typeFullName)
            => Visits.Add($"mod component {typeFullName}");

        public void AcceptEvent<T>(INativeTypeRegistry registry) where T : struct, IGameEvent
            => Visits.Add($"event {typeof(T).Name}");
    }

    private static NativeTypeRegistry Mixed()
        => new([
            new Registration(r => r.RegisterComponent<FirstComponent>().RegisterEvent<FirstEvent>()),
            new Registration(r => r.RegisterComponent<SecondComponent>().RegisterEvent<SecondEvent>()),
        ]);

    private static int[] Ids()
        => [FirstComponent.Id, FirstEvent.Id, SecondComponent.Id, SecondEvent.Id];

    [Fact]
    public void RegisterEventGivesTheSameIdsAsRegisterComponent()
    {
        var asComponents = new NativeTypeRegistry([
            new Registration(r => r.RegisterComponent<FirstComponent>().RegisterComponent<FirstEvent>()),
            new Registration(r => r.RegisterComponent<SecondComponent>().RegisterComponent<SecondEvent>()),
        ]);
        var componentIds = Ids();

        var mixed = Mixed();
        var mixedIds = Ids();

        Assert.Equal([0, 1, 2, 3], mixedIds);
        Assert.Equal(componentIds, mixedIds);
        Assert.Equal(asComponents.GetTypes(), mixed.GetTypes());
        Assert.Equal([typeof(FirstComponent), typeof(FirstEvent), typeof(SecondComponent), typeof(SecondEvent)],
            mixed.GetTypes());
        Assert.Equal(typeof(FirstEvent), mixed.GetTypeById(1));
        Assert.Equal(typeof(SecondEvent), mixed.GetTypeById(3));
        Assert.Null(mixed.GetTypeById(4));
    }

    [Fact]
    public void AcceptVisitsComponentsAndEventsInRegistrationOrder()
    {
        var registry = Mixed();
        var callback = new RecordingCallback();

        registry.Accept(callback);

        Assert.Equal([
            "component FirstComponent",
            "event FirstEvent",
            "component SecondComponent",
            "event SecondEvent",
        ], callback.Visits);
    }

    [Fact]
    public void FilterReplaysEarlierRegistrationsAndSeesLaterOnes()
    {
        var early = new RecordingCallback();
        var late = new RecordingCallback();
        var registry = new NativeTypeRegistry([
            new Registration(r => r.RegisterFilter(early).RegisterComponent<FirstComponent>().RegisterEvent<FirstEvent>()),
            new Registration(r => r.RegisterFilter(late).RegisterEvent<SecondEvent>()),
        ]);

        Assert.Equal(["component FirstComponent", "event FirstEvent", "event SecondEvent"], early.Visits);
        Assert.Equal(["component FirstComponent", "event FirstEvent", "event SecondEvent"], late.Visits);
        Assert.Equal(3, registry.GetTypes().Count);
    }

    private interface ITestComponentRegistry : IComponentRegistryBase<ITestComponentRegistry, ValueType>
    {
        ITestComponentRegistry RegisterComponent<T>() where T : struct;
    }

    private sealed class TestComponentRegistry(IEnumerable<IComponentRegistrationBase<ITestComponentRegistry, ValueType>> registrations)
        : ComponentRegistryBase<ITestComponentRegistry, ValueType>(registrations), ITestComponentRegistry
    {
        public ITestComponentRegistry RegisterComponent<T>() where T : struct
            => RegisterComponentImpl<T>();
    }

    private sealed class TestComponentRegistration(Action<ITestComponentRegistry> register)
        : IComponentRegistrationBase<ITestComponentRegistry, ValueType>
    {
        public void Register(ITestComponentRegistry registry) => register(registry);
    }

    private sealed class NarrowRecordingCallback : IComponentRegistryCallbackBase<ITestComponentRegistry, ValueType>
    {
        public List<string> Visits { get; } = [];

        public void AcceptComponent<T>(ITestComponentRegistry registry, T defaultValue = default) where T : struct
            => Visits.Add(typeof(T).Name);

        public void AcceptModComponent(ITestComponentRegistry registry, ModComponentInfo info, string typeFullName)
            => Visits.Add(typeFullName);
    }

    [Fact]
    public void ComponentOnlyRegistryAcceptsNarrowCallbacks()
    {
        var early = new NarrowRecordingCallback();
        var registry = new TestComponentRegistry([
            new TestComponentRegistration(r => r.RegisterFilter(early).RegisterComponent<FirstComponent>()),
            new TestComponentRegistration(r => r.RegisterComponent<SecondComponent>()),
        ]);
        var late = new NarrowRecordingCallback();
        var visitor = new NarrowRecordingCallback();

        registry.RegisterFilter(late);
        registry.Accept(visitor);

        Assert.Equal(["FirstComponent", "SecondComponent"], early.Visits);
        Assert.Equal(["FirstComponent", "SecondComponent"], late.Visits);
        Assert.Equal(["FirstComponent", "SecondComponent"], visitor.Visits);
    }
}
