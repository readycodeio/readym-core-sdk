using BenchmarkDotNet.Attributes;

namespace ReadyM.SDK.Benchmarks.UseCases;

/// What a loop pays before its body runs once: building the query, then snapshotting what matched.
public class QueryOverheadBenchmarks : BenchmarkWorld
{
    [Benchmark(Baseline = true, Description = "Friflo entities, walk only")]
    public int FrifloWalk()
    {
        var count = 0;

        foreach (var _ in Friflo.Entities)
            count++;

        return count;
    }

    [Benchmark(Description = "v1 snapshot, no body")]
    public int SdkSnapshot()
    {
        var enumerator = Sdk.Query<Creep>().GetEnumerator();
        enumerator.Dispose();
        return 0;
    }
}
