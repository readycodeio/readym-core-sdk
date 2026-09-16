using System;
using BenchmarkDotNet.Attributes;
using Friflo.Engine.ECS;
using FrifloEntity = Friflo.Engine.ECS.Entity;
using IComponent = Friflo.Engine.ECS.IComponent;
using ReadyM.SDK.Client.Entity;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
using SdkEntities = ReadyM.SDK.Client.Entity.Entities;

namespace ReadyM.SDK.Benchmarks.UseCases;

/// The same five reads reached through the interface rather than the concrete api.
internal readonly struct InterfaceApiHero(RawEntity raw, IEntityApi api)
{
    public float Endurance => api.GetComponent<StaminaComponent>(raw).endurance;
    public int Rating => api.GetComponent<ArmorComponent>(raw).rating;
    public float Pace => api.GetComponent<SpeedComponent>(raw).pace;
    public int Gold => api.GetComponent<PurseComponent>(raw).gold;
    public float Spirit => api.GetComponent<MoraleComponent>(raw).spirit;
}

/// <summary>
/// Mirrors ClientEntityApi.GetComponent exactly, plus the same body with the identity resolution
/// already done. The difference between the two arms is the cost of repeating that resolution.
/// </summary>
internal interface IMirrorApi
{
    ref T WithResolve<T>(RawEntity raw) where T : struct, IComponent;

    ref T WithoutResolve<T>(FrifloEntity entity) where T : struct, IComponent;
}

internal sealed class MirrorApi(EntityStore store) : IMirrorApi
{
    public ref T WithResolve<T>(RawEntity raw) where T : struct, IComponent
    {
        var entity = store.GetEntityByRawEntity(raw);

        if (entity.IsNull)
            throw new InvalidOperationException();

        return ref entity.GetComponent<T>();
    }

    public ref T WithoutResolve<T>(FrifloEntity entity) where T : struct, IComponent
    {
        if (entity.IsNull)
            throw new InvalidOperationException();

        return ref entity.GetComponent<T>();
    }
}

/// The same again, but reached through a real EntityHandle instead of holding the api directly.
internal readonly struct HandleHero(EntityHandle handle)
{
    public float Endurance => handle.GetComponent<StaminaComponent>().endurance;
    public int Rating => handle.GetComponent<ArmorComponent>().rating;
    public float Pace => handle.GetComponent<SpeedComponent>().pace;
    public int Gold => handle.GetComponent<PurseComponent>().gold;
    public float Spirit => handle.GetComponent<MoraleComponent>().spirit;
}

/// <summary>
/// Attributes the gap between the concrete-api shape and a real Query&lt;Hero&gt;. Each rung changes
/// exactly one thing against the rung above it, so every delta has a single cause.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class PathAttributionBenchmarks
{
    [Params(10_000)]
    public int Entities { get; set; }

    private IEntities _sdk = null!;
    private ClientEntityApi _concreteApi = null!;
    private IEntityApi _api = null!;
    private IMirrorApi _mirror = null!;
    private ArchetypeQuery<StaminaComponent, ArmorComponent, SpeedComponent, PurseComponent, MoraleComponent> _friflo = null!;

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
        _api = _concreteApi;
        _mirror = new MirrorApi(store);
        _sdk = new SdkEntities(store, _concreteApi);
        _friflo = store.Query<StaminaComponent, ArmorComponent, SpeedComponent, PurseComponent, MoraleComponent>();
    }

    // --- rung A: concrete api, fields read straight off it -----------------------------------

    [Benchmark(Baseline = true, Description = "A concrete api")]
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

    // --- rung B: same, but through the interface. B minus A is dispatch. ----------------------

    [Benchmark(Description = "B + interface dispatch")]
    public float InterfaceApi()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
        {
            var hero = new InterfaceApiHero(entity.RawEntity, _api);
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;
        }

        return total;
    }

    // --- B1/B2: the same interface body, resolving per access then once per entity. -----------

    [Benchmark(Description = "B1 interface, resolve per access")]
    public float MirrorPerAccess()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
        {
            var raw = entity.RawEntity;

            total += _mirror.WithResolve<StaminaComponent>(raw).endurance
                     + _mirror.WithResolve<ArmorComponent>(raw).rating
                     + _mirror.WithResolve<SpeedComponent>(raw).pace
                     + _mirror.WithResolve<PurseComponent>(raw).gold
                     + _mirror.WithResolve<MoraleComponent>(raw).spirit;
        }

        return total;
    }

    [Benchmark(Description = "B2 interface, resolve once per entity")]
    public float MirrorPerEntity()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
            total += _mirror.WithoutResolve<StaminaComponent>(entity).endurance
                     + _mirror.WithoutResolve<ArmorComponent>(entity).rating
                     + _mirror.WithoutResolve<SpeedComponent>(entity).pace
                     + _mirror.WithoutResolve<PurseComponent>(entity).gold
                     + _mirror.WithoutResolve<MoraleComponent>(entity).spirit;

        return total;
    }

    // --- rung C: same, but through EntityHandle. C minus B is the handle wrapper. -------------

    [Benchmark(Description = "C + EntityHandle")]
    public float ViaHandle()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
        {
            var hero = new HandleHero(new EntityHandle(entity.RawEntity, _api));
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;
        }

        return total;
    }

    // --- rung D: the real Hero, so reads go through the generated accessor classes. -----------

    [Benchmark(Description = "D + generated accessors")]
    public float ViaAccessors()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
        {
            var hero = new Hero(new EntityHandle(entity.RawEntity, _api));
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;
        }

        return total;
    }

    // --- rung D2: built the way the enumerator builds it, through the generic init. ------------

    /// Exactly what QueryBuilder's enumerator does to produce each element.
    private static T Make<T>(EntityHandle handle) where T : struct, IArchetypeQueryable
        => new() { Handle = handle };

    [Benchmark(Description = "D2 + generic construction")]
    public float ViaGenericConstruction()
    {
        var total = 0f;

        foreach (var entity in _friflo.Entities)
        {
            var hero = Make<Hero>(new EntityHandle(entity.RawEntity, _api));
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;
        }

        return total;
    }

    // --- rung E: the real query. E minus D is the SDK enumerator and its identity buffer. -----

    [Benchmark(Description = "E + SDK iteration (Query<Hero>)")]
    public float SdkQuery()
    {
        var total = 0f;

        foreach (var hero in _sdk.Query<Hero>())
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;

        return total;
    }

    // --- the two loops on their own, to price iteration without a body -----------------------

    [Benchmark(Description = "loop only: Friflo entities")]
    public int LoopFriflo()
    {
        var count = 0;

        foreach (var _ in _friflo.Entities)
            count++;

        return count;
    }

    [Benchmark(Description = "loop only: Query<Hero>")]
    public int LoopSdk()
    {
        var count = 0;

        foreach (var _ in _sdk.Query<Hero>())
            count++;

        return count;
    }
}
