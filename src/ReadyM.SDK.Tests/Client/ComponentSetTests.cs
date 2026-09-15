using System.Collections.Concurrent;
using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entity;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

public class ComponentSetTests : ClientSdkTest
{
    // Distinct generic tuples, so each is a set no other test has resolved yet.
    private static readonly Func<ComponentSet>[] Sets =
    [
        ComponentSet.Of<LootComponent, HealthComponent>,
        ComponentSet.Of<HealthComponent, LootComponent>,
        ComponentSet.Of<MonsterComponent, ChestComponent>,
        ComponentSet.Of<ChestComponent, MonsterComponent>,
        ComponentSet.Of<PlacementComponent, LootComponent>,
        ComponentSet.Of<LootComponent, PlacementComponent, HealthComponent>
    ];

    [Fact]
    public void An_empty_set_carries_no_types()
    {
        Assert.Empty(ComponentSet.Empty.Types);
    }

    [Fact]
    public void Resolving_a_set_survives_concurrent_first_use()
    {
        // The resolved-component memo is written on first use and keyed on reference identity. A
        // plain Dictionary there corrupted itself when two query sites resolved at the same time.
        var resolved = new ConcurrentBag<(int Index, ComponentTypes Types)>();

        Parallel.For(0, 2048, i =>
        {
            var index = i % Sets.Length;
            resolved.Add((index, ClientComponents.Resolve(Sets[index]())));
        });

        for (var index = 0; index < Sets.Length; index++)
        {
            var expected = ClientComponents.Resolve(Sets[index]());

            foreach (var (_, types) in resolved.Where(entry => entry.Index == index))
            {
                Assert.True(types.HasAll(expected));
                Assert.True(expected.HasAll(types));
            }
        }
    }

    [Fact]
    public void Resolving_an_unregistered_component_names_it()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => ClientComponents.Resolve(ComponentSet.Of<NotAComponent>()));

        Assert.Contains(nameof(NotAComponent), error.Message);
    }

    private struct NotAComponent
    {
        public int value;
    }
}
