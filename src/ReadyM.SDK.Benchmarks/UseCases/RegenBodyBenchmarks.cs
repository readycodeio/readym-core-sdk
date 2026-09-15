using BenchmarkDotNet.Attributes;

namespace ReadyM.SDK.Benchmarks.UseCases;

/// Five accessor touches per entity, which is what shows that the SDK's cost is per access rather
/// than per entity.
public class RegenBodyBenchmarks : BenchmarkWorld
{
    [Benchmark(Baseline = true, Description = "Friflo chunks")]
    public float FrifloChunks()
    {
        var total = 0f;

        foreach (var (vitality, _) in Friflo.Chunks)
        {
            var span = vitality.Span;

            for (var i = 0; i < span.Length; i++)
            {
                if (span[i].hp < span[i].maxHp)
                    span[i].hp += 1f;

                total += span[i].hp;
            }
        }

        return total;
    }

    [Benchmark(Description = "Friflo ForEachEntity")]
    public float FrifloForEachEntity()
    {
        Sink = 0f;
        Friflo.ForEachEntity(RegenLambda);
        return Sink;
    }

    [Benchmark(Description = "v0 wrapper")]
    public float V0Wrapper()
    {
        var total = 0f;

        foreach (var entity in Friflo.Entities)
        {
            var creep = new V0Creep(entity);

            if (creep.Hp < creep.MaxHp)
                creep.Hp += 1f;

            total += creep.Hp;
        }

        return total;
    }

    [Benchmark(Description = "v1 Query<Creep>")]
    public float SdkArchetypeQuery()
    {
        var total = 0f;

        foreach (var creep in Sdk.Query<Creep>())
        {
            if (creep.Hp < creep.MaxHp)
                creep.Hp += 1f;

            total += creep.Hp;
        }

        return total;
    }
}
