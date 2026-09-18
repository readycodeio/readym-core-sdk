using BenchmarkDotNet.Attributes;
using Friflo.Engine.ECS;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using ClientEntities = ReadyM.SDK.Client.Entities.Entities;
using FrifloEntity = Friflo.Engine.ECS.Entity;

namespace ReadyM.SDK.Benchmarks;

/// <summary>
/// Reading one component off one entity you already hold, repeatedly.
/// </summary>
/// <remarks>
/// No iteration and no query, so this is the per-access cost of each generation's entity reference
/// and nothing else. In-process Friflo, because that is where all three are literally comparable.
/// </remarks>
[MemoryDiagnoser]
// The generated per-run project binds a different Friflo than the project reference does, so
// everything here runs in the host process. See the note in README.
[InProcess]
public class EntityAccessBenchmarks
{
    [Params(10_000)]
    public int Accesses { get; set; }

    private FrifloEntity _friflo;
    private V0Creep _v0;
    private Creep _v1;

    [GlobalSetup]
    public void Setup()
    {
        var store = new EntityStore();
        var sdk = new ClientEntities(store, new ClientEntityApi(store));

        _v1 = sdk.Create<Creep>();
        _v1.Hp = 1f;

        _friflo = store.GetEntityByRawEntity(EntityHandle.Of(_v1).RawEntity);
        _v0 = new V0Creep(_friflo);
    }

    [Benchmark(Baseline = true, Description = "Friflo entity")]
    public float Friflo()
    {
        var total = 0f;

        for (var i = 0; i < Accesses; i++)
            total += _friflo.GetComponent<VitalityComponent>().hp;

        return total;
    }

    [Benchmark(Description = "v0 wrapper")]
    public float V0()
    {
        var total = 0f;

        for (var i = 0; i < Accesses; i++)
            total += _v0.Hp;

        return total;
    }

    [Benchmark(Description = "v1 archetype")]
    public float V1()
    {
        var total = 0f;

        for (var i = 0; i < Accesses; i++)
            total += _v1.Hp;

        return total;
    }
}
