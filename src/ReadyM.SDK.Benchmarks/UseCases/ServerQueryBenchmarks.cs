using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.TestHarness;
using ServerEntities = ReadyM.SDK.Server.Entity.Entities;

namespace ReadyM.SDK.Benchmarks.UseCases;

/// <summary>
/// The server half of the five-component case, over a fake relay reached through real function
/// pointers, so the marshalling the mod host really pays is inside the measurement.
/// </summary>
/// <remarks>
/// The chunk arm is hand-written and stands in for the fast path that does not exist yet: it is what
/// the SDK would cost if a query crossed the boundary once and walked the chunks it got back. The
/// same Hero declaration drives both arms and the client benchmarks, which is the point of the
/// generated accessors being side-neutral.
/// </remarks>
[MemoryDiagnoser]
[InProcess]
public class ServerQueryBenchmarks
{
    [Params(10_000)]
    public int Entities { get; set; }

    private FakeRelay _relay = null!;
    private IEntities _sdk = null!;
    private QueryDelegate _query = null!;
    private int[] _componentIds = null!;
    private float _sink;

    [GlobalSetup]
    public void Setup()
    {
        _relay = new FakeRelay();

        _relay.RegisterBlittable<StaminaComponent>();
        _relay.RegisterBlittable<ArmorComponent>();
        _relay.RegisterBlittable<SpeedComponent>();
        _relay.RegisterManaged<PurseComponent>();
        _relay.RegisterManaged<MoraleComponent>();

        var components = new[]
        {
            typeof(StaminaComponent), typeof(ArmorComponent), typeof(SpeedComponent),
            typeof(PurseComponent), typeof(MoraleComponent)
        };

        for (var i = 0; i < Entities; i++)
        {
            var entity = _relay.Create(components);

            _relay.Set(entity, new StaminaComponent { endurance = 1f });
            _relay.Set(entity, new ArmorComponent { rating = 2 });
            _relay.Set(entity, new SpeedComponent { pace = 3f });
            _relay.Set(entity, new PurseComponent { gold = 4 });
            _relay.Set(entity, new MoraleComponent { spirit = 5f });
        }

        _sdk = new ServerEntities(_relay.CreateEntityApi());
        _query = Marshal.GetDelegateForFunctionPointer<QueryDelegate>(_relay.Pointers.Query);

        _componentIds =
        [
            _relay.ComponentId<StaminaComponent>(), _relay.ComponentId<ArmorComponent>(),
            _relay.ComponentId<SpeedComponent>(), _relay.ComponentId<PurseComponent>(),
            _relay.ComponentId<MoraleComponent>()
        ];
    }

    /// One crossing for the whole query, then straight reads off the chunks it handed back.
    [Benchmark(Baseline = true, Description = "chunk walk (what the fast path would be)")]
    public unsafe float ChunkWalk()
    {
        _sink = 0f;

        fixed (int* ids = _componentIds)
            _query(ids, _componentIds.Length, Accumulate);

        return _sink;
    }

    /// Exactly what a mod writes. The shape is named, so the loop binds the chunk path.
    [Benchmark(Description = "v1 Query<Hero>")]
    public float SdkQuery()
    {
        var total = 0f;

        foreach (var hero in _sdk.Query<Hero>())
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;

        return total;
    }

    /// Two mixins, no archetype naming both. Same path, same single crossing.
    [Benchmark(Description = "v1 Query<Stamina, Armor>")]
    public float SdkPairQuery()
    {
        var total = 0f;

        foreach (var (stamina, armor) in _sdk.Query<Stamina, Armor>())
            total += stamina.Endurance + armor.Rating;

        return total;
    }

    /// Five mixins asked for directly, against the archetype that names the same five.
    [Benchmark(Description = "v1 Query<x5 mixins>")]
    public float SdkFiveShapeQuery()
    {
        var total = 0f;

        foreach (var (stamina, armor, speed, purse, morale)
                 in _sdk.Query<Stamina, Armor, Speed, Purse, Morale>())
            total += stamina.Endurance + armor.Rating + speed.Pace + purse.Gold + morale.Spirit;

        return total;
    }

    /// The same query walked by identity, which is what a shape without a chunk view still gets.
    [Benchmark(Description = "v1 Query<Hero>, walked by identity")]
    public float SdkIdentities()
    {
        var total = 0f;
        var entities = _sdk.Query<Hero>().Identities();

        while (entities.MoveNext())
        {
            var hero = entities.Current;
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;
        }

        entities.Dispose();
        return total;
    }

    /// Empty body, so this is what binding a view per chunk and copying it per entity costs.
    [Benchmark(Description = "v1 Query<Hero>, empty body")]
    public int SdkLoopOnly()
    {
        var count = 0;

        foreach (var _ in _sdk.Query<Hero>())
            count++;

        return count;
    }

    private unsafe void Accumulate(IntPtr entities, ChunkComponent* comps, int n, int count)
    {
        ref var stamina = ref Chunks.Base<StaminaComponent>(comps[0]);
        ref var armor = ref Chunks.Base<ArmorComponent>(comps[1]);
        ref var speed = ref Chunks.Base<SpeedComponent>(comps[2]);
        ref var purse = ref Chunks.Base<PurseComponent>(comps[3]);
        ref var morale = ref Chunks.Base<MoraleComponent>(comps[4]);

        var total = 0f;

        for (var i = 0; i < count; i++)
            total += Chunks.At(ref stamina, i, comps[0].Stride).endurance
                     + Chunks.At(ref armor, i, comps[1].Stride).rating
                     + Chunks.At(ref speed, i, comps[2].Stride).pace
                     + Chunks.At(ref purse, i, comps[3].Stride).gold
                     + Chunks.At(ref morale, i, comps[4].Stride).spirit;

        _sink += total;
    }
}
