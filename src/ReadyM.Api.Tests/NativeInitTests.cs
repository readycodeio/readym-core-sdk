using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using Xunit;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.Api.Tests;

/// <summary>
/// Native init used to be dispatched through a reflected delegate, which a trimmed or AOT build
/// removed, so components shipped with their native fields unallocated and only a warning to say so.
/// These pin the behaviour to an interface call, which cannot be trimmed away.
/// </summary>
public sealed class NativeInitTests
{
    [StructLayout(LayoutKind.Auto)]
    private struct ListComponent : IComponent, INativeInit
    {
        public NativeList<int> Items;

        public void Init(AllocatorKind allocatorKind)
        {
            Items = new NativeList<int>(4, allocatorKind);
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private struct PlainComponent : IComponent
    {
        public int Value;
    }

    private sealed class Registration(Action<IArchetypeRegistry> configure) : IArchetypeRegistration
    {
        public void Register(IArchetypeRegistry registry) => configure(registry);
    }

    private static Store CreateStore(Action<IArchetypeRegistry> configure)
    {
        var store = new Store(new EntityStore(), NullLogger.Instance, [new Registration(configure)]);
        store.SetThread(Thread.CurrentThread);
        return store;
    }

    [Fact]
    public void NativeFieldsAreAllocatedOnEntityCreation()
    {
        ArchetypeId archetype = default;
        var store = CreateStore(r => archetype = r.RegisterArchetype(new ArchetypeBuilder().Add<ListComponent>()));

        var entity = store.CreateEntity(archetype);

        Assert.True(entity.GetComponent<ListComponent>().Items.IsCreated);
    }

    [Fact]
    public void AllocatedListIsUsable()
    {
        ArchetypeId archetype = default;
        var store = CreateStore(r => archetype = r.RegisterArchetype(new ArchetypeBuilder().Add<ListComponent>()));

        var entity = store.CreateEntity(archetype);
        entity.GetComponent<ListComponent>().Items.Add(7);

        var items = entity.GetComponent<ListComponent>().Items;
        Assert.Equal(1, items.Count);
        Assert.Equal(7, items[0]);
    }

    [Fact]
    public void ComponentsWithoutTheInterfaceAreLeftAlone()
    {
        ArchetypeId archetype = default;
        var store = CreateStore(r => archetype = r.RegisterArchetype(new ArchetypeBuilder().Add<PlainComponent>()));

        var entity = store.CreateEntity(archetype);

        Assert.Equal(0, entity.GetComponent<PlainComponent>().Value);
    }

    [Fact]
    public void DefaultValueDoesNotOverwriteTheAllocation()
    {
        // The default-value handler has to run before native init, or it assigns a fresh struct over
        // the allocated one and the list goes back to being uncreated.
        ArchetypeId archetype = default;
        var store = CreateStore(r =>
            archetype = r.RegisterArchetype(new ArchetypeBuilder().Add(new ListComponent())));

        var entity = store.CreateEntity(archetype);

        Assert.True(entity.GetComponent<ListComponent>().Items.IsCreated);
    }
}
