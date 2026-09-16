using BenchmarkDotNet.Attributes;

namespace ReadyM.SDK.Benchmarks.Probes;

/// <summary>
/// The same four dispatch shapes, but every call must first resolve the identity, which is what
/// both halves really do. Five accesses share one entity, so whether the resolve is paid once or
/// five times depends on the call being inlined.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ResolveDispatchBenchmarks
{
    [Params(10_000)]
    public int Entities { get; set; }

    private ProbeHeap<P1> _h1 = null!;
    private ProbeHeap<P2> _h2 = null!;
    private ProbeHeap<P3> _h3 = null!;
    private ProbeHeap<P4> _h4 = null!;
    private ProbeHeap<P5> _h5 = null!;

    private ProbeApi _concrete = null!;
    private IProbeApi _api = null!;

    [GlobalSetup]
    public void Setup()
    {
        _h1 = new ProbeHeap<P1>(Entities);
        _h2 = new ProbeHeap<P2>(Entities);
        _h3 = new ProbeHeap<P3>(Entities);
        _h4 = new ProbeHeap<P4>(Entities);
        _h5 = new ProbeHeap<P5>(Entities);

        ProbeId<P1>.Value = 0;
        ProbeId<P2>.Value = 1;
        ProbeId<P3>.Value = 2;
        ProbeId<P4>.Value = 3;
        ProbeId<P5>.Value = 4;

        for (var i = 0; i < Entities; i++)
        {
            _h1.Items[i].Value = 1f;
            _h2.Items[i].Value = 1f;
            _h3.Items[i].Value = 1f;
            _h4.Items[i].Value = 1f;
            _h5.Items[i].Value = 1f;
        }

        _concrete = new ProbeApi([_h1, _h2, _h3, _h4, _h5], new short[Entities]);
        _api = _concrete;
    }

    [Benchmark(Baseline = true, Description = "direct array")]
    public float Direct()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            total += _h1.Items[i].Value;
            total += _h2.Items[i].Value;
            total += _h3.Items[i].Value;
            total += _h4.Items[i].Value;
            total += _h5.Items[i].Value;
        }

        return total;
    }

    [Benchmark(Description = "resolve + generic, concrete class")]
    public float GenericConcrete()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            var entity = new ProbeEntity(i, 0);
            var handle = new ConcreteHandle(_concrete, i);

            total += handle.GenericResolved<P1>(entity).Value;
            total += handle.GenericResolved<P2>(entity).Value;
            total += handle.GenericResolved<P3>(entity).Value;
            total += handle.GenericResolved<P4>(entity).Value;
            total += handle.GenericResolved<P5>(entity).Value;
        }

        return total;
    }

    [Benchmark(Description = "resolve + non-generic, concrete class")]
    public float NonGenericConcrete()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            var entity = new ProbeEntity(i, 0);
            var handle = new ConcreteHandle(_concrete, i);

            total += handle.NonGenericResolved<P1>(entity).Value;
            total += handle.NonGenericResolved<P2>(entity).Value;
            total += handle.NonGenericResolved<P3>(entity).Value;
            total += handle.NonGenericResolved<P4>(entity).Value;
            total += handle.NonGenericResolved<P5>(entity).Value;
        }

        return total;
    }

    [Benchmark(Description = "resolve + generic interface (today)")]
    public float GenericInterface()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            var entity = new ProbeEntity(i, 0);
            var handle = new InterfaceHandle(_api, i);

            total += handle.GenericResolved<P1>(entity).Value;
            total += handle.GenericResolved<P2>(entity).Value;
            total += handle.GenericResolved<P3>(entity).Value;
            total += handle.GenericResolved<P4>(entity).Value;
            total += handle.GenericResolved<P5>(entity).Value;
        }

        return total;
    }

    [Benchmark(Description = "resolve + non-generic interface (proposed)")]
    public float NonGenericInterface()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            var entity = new ProbeEntity(i, 0);
            var handle = new InterfaceHandle(_api, i);

            total += handle.NonGenericResolved<P1>(entity).Value;
            total += handle.NonGenericResolved<P2>(entity).Value;
            total += handle.NonGenericResolved<P3>(entity).Value;
            total += handle.NonGenericResolved<P4>(entity).Value;
            total += handle.NonGenericResolved<P5>(entity).Value;
        }

        return total;
    }
}
