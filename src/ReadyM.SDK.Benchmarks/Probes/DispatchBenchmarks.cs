using BenchmarkDotNet.Attributes;

namespace ReadyM.SDK.Benchmarks.Probes;

/// <summary>
/// Isolates call dispatch. Every arm reaches the same array element through the same number of
/// loads, so the only difference between them is how the call is made. No ECS, no entity store.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DispatchBenchmarks
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

    [Benchmark(Description = "generic, concrete class")]
    public float GenericConcrete()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            var handle = new ConcreteHandle(_concrete, i);

            total += handle.Generic<P1>().Value;
            total += handle.Generic<P2>().Value;
            total += handle.Generic<P3>().Value;
            total += handle.Generic<P4>().Value;
            total += handle.Generic<P5>().Value;
        }

        return total;
    }

    [Benchmark(Description = "non-generic, concrete class")]
    public float NonGenericConcrete()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            var handle = new ConcreteHandle(_concrete, i);

            total += handle.NonGeneric<P1>().Value;
            total += handle.NonGeneric<P2>().Value;
            total += handle.NonGeneric<P3>().Value;
            total += handle.NonGeneric<P4>().Value;
            total += handle.NonGeneric<P5>().Value;
        }

        return total;
    }

    [Benchmark(Description = "generic interface (today)")]
    public float GenericInterface()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            var handle = new InterfaceHandle(_api, i);

            total += handle.Generic<P1>().Value;
            total += handle.Generic<P2>().Value;
            total += handle.Generic<P3>().Value;
            total += handle.Generic<P4>().Value;
            total += handle.Generic<P5>().Value;
        }

        return total;
    }

    [Benchmark(Description = "non-generic interface (proposed)")]
    public float NonGenericInterface()
    {
        var total = 0f;

        for (var i = 0; i < Entities; i++)
        {
            var handle = new InterfaceHandle(_api, i);

            total += handle.NonGeneric<P1>().Value;
            total += handle.NonGeneric<P2>().Value;
            total += handle.NonGeneric<P3>().Value;
            total += handle.NonGeneric<P4>().Value;
            total += handle.NonGeneric<P5>().Value;
        }

        return total;
    }
}
