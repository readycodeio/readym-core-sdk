using BenchmarkDotNet.Attributes;

namespace ReadyM.SDK.Benchmarks.Probes;

/// <summary>
/// What an archetype costs as a struct against an abstract class, which is the trade an abstract
/// base would buy: adding a virtual member never breaks a derived type, on any runtime.
/// </summary>
/// <remarks>
/// A query hands out one of these per entity, so the comparison is per entity rather than per
/// access. Both arms read the same array through the same number of loads.
/// </remarks>
[MemoryDiagnoser]
[InProcess]
public class ShapeKindBenchmarks
{
    [Params(10_000)]
    public int Entities { get; set; }

    private float[] _values = null!;

    [GlobalSetup]
    public void Setup()
    {
        _values = new float[Entities];

        for (var i = 0; i < Entities; i++)
            _values[i] = 1f;
    }

    // -- what an archetype is today ------------------------------------------------------------

    private interface IShape
    {
        float Value { get; }
    }

    private readonly struct StructShape(float[] values, int index) : IShape
    {
        public float Value => values[index];
    }

    // -- what it would have to be to inherit from an abstract base -----------------------------

    private abstract class ShapeBase
    {
        public abstract float Value { get; }

        /// A member added here later costs a derived type nothing, which is the whole appeal.
        public virtual bool IsValid => true;
    }

    private sealed class ClassShape(float[] values, int index) : ShapeBase
    {
        public override float Value => values[index];
    }

    [Benchmark(Baseline = true, Description = "struct, called directly")]
    public float StructDirect()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
            total += new StructShape(_values, i).Value;

        return total;
    }

    /// How a query body really reaches it: a type parameter, so the JIT specialises and devirtualises.
    [Benchmark(Description = "struct, through a generic constraint")]
    public float StructGeneric()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
            total += Read(new StructShape(_values, i));

        return total;
    }

    private static float Read<T>(T shape) where T : struct, IShape => shape.Value;

    [Benchmark(Description = "class over an abstract base")]
    public float ClassVirtual()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
            total += new ClassShape(_values, i).Value;

        return total;
    }

    /// The same class reached through the base, which is what generic query code would have to do.
    [Benchmark(Description = "class through the base type")]
    public float ClassThroughBase()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            ShapeBase shape = new ClassShape(_values, i);
            total += shape.Value;
        }

        return total;
    }
}
