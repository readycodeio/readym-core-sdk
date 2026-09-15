using BenchmarkDotNet.Attributes;

namespace ReadyM.SDK.Benchmarks.UseCases;

/// One accessor touched per entity, which isolates the per-entity cost of each layer.
public class SingleReadBenchmarks : BenchmarkWorld
{
    [Benchmark(Baseline = true, Description = "Friflo chunks")]
    public float FrifloChunks()
    {
        var total = 0f;

        foreach (var (vitality, _) in Friflo.Chunks)
        {
            var span = vitality.Span;

            for (var i = 0; i < span.Length; i++)
                total += span[i].hp;
        }

        return total;
    }

    [Benchmark(Description = "Friflo entities")]
    public float FrifloEntities()
    {
        var total = 0f;

        foreach (var entity in Friflo.Entities)
            total += entity.GetComponent<VitalityComponent>().hp;

        return total;
    }

    [Benchmark(Description = "Friflo ForEachEntity")]
    public float FrifloForEachEntity()
    {
        Sink = 0f;
        Friflo.ForEachEntity(ReadLambda);
        return Sink;
    }

    [Benchmark(Description = "v0 wrapper")]
    public float V0Wrapper()
    {
        var total = 0f;

        foreach (var entity in Friflo.Entities)
            total += new V0Creep(entity).Hp;

        return total;
    }

    [Benchmark(Description = "v1 Query<Creep>")]
    public float SdkArchetypeQuery()
    {
        var total = 0f;

        foreach (var creep in Sdk.Query<Creep>())
            total += creep.Hp;

        return total;
    }

    [Benchmark(Description = "v1 Query<Vitality> mixin")]
    public float SdkMixinQuery()
    {
        var total = 0f;

        foreach (var vitality in Sdk.Query<Vitality>())
            total += vitality.Hp;

        return total;
    }
}
