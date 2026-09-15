using BenchmarkDotNet.Attributes;
using Friflo.Engine.ECS;
using ReadyM.SDK.Client.Entity;
using SdkEntities = ReadyM.SDK.Client.Entity.Entities;

namespace ReadyM.SDK.Benchmarks.UseCases;

/// <summary>
/// One accessor on each of five components, which is what an archetype built from several mixins
/// actually looks like.
/// </summary>
/// <remarks>
/// Its own world, so the heroes do not widen the archetype scan the one-component benchmarks do.
/// </remarks>
[MemoryDiagnoser]
[ShortRunJob]
public class FiveComponentBenchmarks
{
    [Params(10_000)]
    public int Entities { get; set; }

    private IEntities _sdk = null!;
    private ClientEntityApi _concreteApi = null!;
    private ArchetypeQuery<StaminaComponent, ArmorComponent, SpeedComponent, PurseComponent, MoraleComponent> _friflo = null!;
    private ForEachEntity<StaminaComponent, ArmorComponent, SpeedComponent, PurseComponent, MoraleComponent> _lambda = null!;
    private float _sink;

    [GlobalSetup]
    public void Setup()
    {
        var store = new EntityStore();

        for (var i = 0; i < Entities; i++)
            store.CreateEntity(
                new StaminaComponent { endurance = 1f },
                new ArmorComponent { rating = 2 },
                new SpeedComponent { pace = 3f },
                new PurseComponent { gold = 4 },
                new MoraleComponent { spirit = 5f });

        _concreteApi = new ClientEntityApi(store);
        _sdk = new SdkEntities(store, _concreteApi);
        _friflo = store.Query<StaminaComponent, ArmorComponent, SpeedComponent, PurseComponent, MoraleComponent>();

        // Cached, so the delegate allocation does not land inside the measured region.
        _lambda = (ref stamina, ref armor, ref speed, ref purse, ref morale, _) =>
            _sink += stamina.endurance + armor.rating + speed.pace + purse.gold + morale.spirit;
    }

    [Benchmark(Baseline = true, Description = "Friflo chunks")]
    public float FrifloChunks()
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
                total += staminas[i].endurance + armors[i].rating + speeds[i].pace + purses[i].gold + morales[i].spirit;
        }

        return total;
    }

    [Benchmark(Description = "Friflo ForEachEntity")]
    public float FrifloForEachEntity()
    {
        _sink = 0f;
        _friflo.ForEachEntity(_lambda);
        return _sink;
    }

    [Benchmark(Description = "Friflo entities")]
    public float FrifloEntities()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
            total += entity.GetComponent<StaminaComponent>().endurance
                     + entity.GetComponent<ArmorComponent>().rating
                     + entity.GetComponent<SpeedComponent>().pace
                     + entity.GetComponent<PurseComponent>().gold
                     + entity.GetComponent<MoraleComponent>().spirit;

        return total;
    }

    [Benchmark(Description = "v0 wrapper")]
    public float V0Wrapper()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
        {
            var hero = new V0Hero(entity);
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;
        }

        return total;
    }

    [Benchmark(Description = "v1 shape, concrete api")]
    public float ConcreteApi()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
        {
            var hero = new ConcreteApiHero(entity.RawEntity, _concreteApi);
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;
        }

        return total;
    }

    [Benchmark(Description = "v1 Query<Hero>")]
    public float SdkArchetypeQuery()
    {
        var total = 0f;

        foreach (var hero in _sdk.Query<Hero>())
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;

        return total;
    }
}
