using BenchmarkDotNet.Attributes;
using Friflo.Engine.ECS;
using ReadyM.SDK.Client.Entity;
using ClientEntities = ReadyM.SDK.Client.Entity.Entities;

namespace ReadyM.SDK.Benchmarks;

/// <summary>
/// A query over one archetype on the client, reading five fields from the five mixins it includes.
/// The same work as the server's archetype comparison, in the half where all three generations are
/// literally comparable and there is no interop boundary to dominate the cost.
/// </summary>
[MemoryDiagnoser]
[InProcess]
public class ClientQueryBenchmarks
{
    [Params(10_000)]
    public int Entities { get; set; }

    private EntityStore _store = null!;
    private IEntities _sdk = null!;
    private ArchetypeQuery<StaminaComponent, ArmorComponent, SpeedComponent, PurseComponent, MoraleComponent> _friflo = null!;

    [GlobalSetup]
    public void Setup()
    {
        _store = new EntityStore();
        _sdk = new ClientEntities(_store, new ClientEntityApi(_store));

        for (var i = 0; i < Entities; i++)
        {
            var hero = _sdk.Create<Hero>();

            hero.Endurance = 1f;
            hero.Rating = 2;
            hero.Pace = 3f;
            hero.Gold = 4;
            hero.Spirit = 5f;
        }

        _friflo = _store.Query<StaminaComponent, ArmorComponent, SpeedComponent, PurseComponent, MoraleComponent>();
    }

    [Benchmark(Baseline = true, Description = "Friflo chunks")]
    public float Friflo()
    {
        var total = 0f;

        foreach (var (stamina, armor, speed, purse, morale, _) in _friflo.Chunks)
        {
            var staminas = stamina.Span;
            var armors = armor.Span;
            var speeds = speed.Span;
            var purses = purse.Span;
            var morales = morale.Span;

            for (var i = 0; i < staminas.Length; i++)
                total += staminas[i].endurance + armors[i].rating + speeds[i].pace
                         + purses[i].gold + morales[i].spirit;
        }

        return total;
    }

    /// The v0 shape: one wrapper per entity, every field read resolving the entity again.
    [Benchmark(Description = "v0 wrapper")]
    public float V0()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
        {
            var hero = new V0Hero(entity);
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;
        }

        return total;
    }

    [Benchmark(Description = "v1")]
    public float V1()
    {
        var total = 0f;

        foreach (var hero in _sdk.Query<Hero>())
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;

        return total;
    }
}
