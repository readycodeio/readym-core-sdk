using BenchmarkDotNet.Attributes;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.SDK.TestHarness;

namespace ReadyM.SDK.Benchmarks;

/// <summary>A query over one mixin, reading its one field.</summary>
public class MixinQueryBenchmarks : ServerWorld
{
    private int[] _ids = null!;
    private V0Body<StaminaComponent> _v0Body = null!;
    private ChunkCallback _v0Chunk = null!;
    private float _sink;

    [GlobalSetup(Target = nameof(Friflo) + "," + nameof(V0Query) + "," + nameof(V1))]
    public unsafe void Prepare()
    {
        Setup();
        _ids = ComponentIds<StaminaComponent>();

        // Cached, so the delegate allocation stays out of the measured region.
        _v0Body = (ref StaminaComponent stamina) => _sink += stamina.endurance;

        // v0's loop: bases once per chunk, then a delegate per entity.
        _v0Chunk = (entities, comps, n, count) =>
        {
            ref var stamina = ref Chunks.Base<StaminaComponent>(comps[0]);

            for (var i = 0; i < count; i++)
                _v0Body(ref Chunks.At(ref stamina, i, comps[0].Stride));
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
        ref var stamina = ref Chunks.Base<StaminaComponent>(comps[0]);
        var total = 0f;

        for (var i = 0; i < count; i++)
            total += Chunks.At(ref stamina, i, comps[0].Stride).endurance;

        _sink += total;
    }

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

        foreach (var stamina in Sdk.Query<Stamina>())
            total += stamina.Endurance;

        return total;
    }
}
