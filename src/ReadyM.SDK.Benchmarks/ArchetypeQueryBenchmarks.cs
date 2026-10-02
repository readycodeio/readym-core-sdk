using BenchmarkDotNet.Attributes;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.SDK.TestHarness;

namespace ReadyM.SDK.Benchmarks;

/// <summary>
/// A query over one archetype, reading five fields from the five mixins it includes.
/// </summary>
public class ArchetypeQueryBenchmarks : ServerWorld
{
    private int[] _ids = null!;
    private V0Body<StaminaComponent, ArmorComponent, SpeedComponent, PurseComponent, MoraleComponent> _v0Body = null!;
    private ChunkCallback _v0Chunk = null!;
    private float _sink;

    [GlobalSetup(Target = nameof(Friflo) + "," + nameof(V0Query) + "," + nameof(V1))]
    public unsafe void Prepare()
    {
        Setup();
        _ids = ComponentIds<StaminaComponent, ArmorComponent, SpeedComponent, PurseComponent, MoraleComponent>();

        // Cached, so the delegate allocation stays out of the measured region.
        _v0Body = (ref StaminaComponent stamina, ref ArmorComponent armor, ref SpeedComponent speed,
                   ref PurseComponent purse, ref MoraleComponent morale)
            => _sink += stamina.endurance + armor.rating + speed.pace + purse.gold + morale.spirit;

        // v0's loop: bases once per chunk, then a delegate per entity.
        _v0Chunk = (entities, comps, n, count) =>
        {
            ref var stamina = ref ChunkAccess.Base<StaminaComponent>(comps[0]);
            ref var armor = ref ChunkAccess.Base<ArmorComponent>(comps[1]);
            ref var speed = ref ChunkAccess.Base<SpeedComponent>(comps[2]);
            ref var purse = ref ChunkAccess.Base<PurseComponent>(comps[3]);
            ref var morale = ref ChunkAccess.Base<MoraleComponent>(comps[4]);

            for (var i = 0; i < count; i++)
                _v0Body(
                    ref ChunkAccess.At(ref stamina, i, comps[0].Stride),
                    ref ChunkAccess.At(ref armor, i, comps[1].Stride),
                    ref ChunkAccess.At(ref speed, i, comps[2].Stride),
                    ref ChunkAccess.At(ref purse, i, comps[3].Stride),
                    ref ChunkAccess.At(ref morale, i, comps[4].Stride));
        };

    }

    [Benchmark(Baseline = true, Description = "relay chunks")]
    public unsafe float Friflo()
    {
        _sink = 0f;
        WalkChunks(_ids, Accumulate);
        return _sink;
    }

    private unsafe void Accumulate(IntPtr entities, ChunkComponent* comps, int n, int count)
    {
        ref var stamina = ref ChunkAccess.Base<StaminaComponent>(comps[0]);
        ref var armor = ref ChunkAccess.Base<ArmorComponent>(comps[1]);
        ref var speed = ref ChunkAccess.Base<SpeedComponent>(comps[2]);
        ref var purse = ref ChunkAccess.Base<PurseComponent>(comps[3]);
        ref var morale = ref ChunkAccess.Base<MoraleComponent>(comps[4]);

        var total = 0f;

        for (var i = 0; i < count; i++)
            total += ChunkAccess.At(ref stamina, i, comps[0].Stride).endurance
                     + ChunkAccess.At(ref armor, i, comps[1].Stride).rating
                     + ChunkAccess.At(ref speed, i, comps[2].Stride).pace
                     + ChunkAccess.At(ref purse, i, comps[3].Stride).gold
                     + ChunkAccess.At(ref morale, i, comps[4].Stride).spirit;

        _sink += total;
    }

    /// The develop-branch SDK: one crossing, then a delegate per entity with the components by ref.
    [Benchmark(Description = "v0 EcsApi.Query")]
    public float V0Query()
    {
        _sink = 0f;
        WalkChunks(_ids, _v0Chunk);
        return _sink;
    }

    [Benchmark(Description = "v1")]
    public float V1()
    {
        var total = 0f;

        foreach (var hero in Sdk.Query<Hero>())
            total += hero.Endurance + hero.Rating + hero.Pace + hero.Gold + hero.Spirit;

        return total;
    }
}
