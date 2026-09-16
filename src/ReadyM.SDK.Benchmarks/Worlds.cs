using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.SDK.TestHarness;
using ServerEntities = ReadyM.SDK.Server.Entity.Entities;

namespace ReadyM.SDK.Benchmarks;

/// <summary>
/// A world behind the interop boundary, reached through real function pointers.
/// </summary>
/// <remarks>
/// The server is where the query comparisons belong: it is the half whose cost is dominated by
/// crossing the boundary, and the only one with queries over more than two shapes. The relay's own
/// world is a Friflo world, so the closest a mod can get to raw Friflo is walking the chunks it
/// hands back, which is what the first arm of each query comparison does.
/// </remarks>
[MemoryDiagnoser]
[InProcess]
public abstract class ServerWorld
{
    [Params(10_000)]
    public int Entities { get; set; }

    internal FakeRelay Relay = null!;
    internal Server.Entity.IEntities Sdk = null!;


    private QueryDelegate _query = null!;

    [GlobalSetup]
    public void Setup()
    {
        Relay = new FakeRelay();

        Relay.RegisterBlittable<StaminaComponent>();
        Relay.RegisterBlittable<ArmorComponent>();
        Relay.RegisterBlittable<SpeedComponent>();
        Relay.RegisterManaged<PurseComponent>();
        Relay.RegisterManaged<MoraleComponent>();
        Relay.RegisterBlittable<HeroArchetypeMarker>();

        Sdk = new ServerEntities(Relay.CreateEntityApi());
        _query = Marshal.GetDelegateForFunctionPointer<QueryDelegate>(Relay.Pointers.Query);

        for (var i = 0; i < Entities; i++)
        {
            var hero = Sdk.Create<Hero>();

            hero.Endurance = 1f;
            hero.Rating = 2;
            hero.Pace = 3f;
            hero.Gold = 4;
            hero.Spirit = 5f;
        }
    }

    /// <summary>Runs a query and walks its chunks directly, which is the floor for any of this.</summary>
    internal unsafe void WalkChunks(int[] componentIds, ChunkCallback body)
    {
        fixed (int* ids = componentIds)
            _query(ids, componentIds.Length, body);
    }

    internal int[] ComponentIds<T1>() where T1 : struct
        => [Relay.ComponentId<T1>()];

    internal int[] ComponentIds<T1, T2, T3, T4, T5>()
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
        => [
            Relay.ComponentId<T1>(), Relay.ComponentId<T2>(), Relay.ComponentId<T3>(),
            Relay.ComponentId<T4>(), Relay.ComponentId<T5>()
        ];
}

/// <summary>
/// The shape of a v0 query body: the components of one entity, by reference.
/// </summary>
/// <remarks>
/// v0's generated <c>EcsApi.Query</c> runs one interop call, resolves each component's chunk base
/// once per chunk, and then invokes a delegate like this per entity. The arms below reproduce that
/// rather than calling the real thing, whose thread-static callback bookkeeping is not what is being
/// measured. Everything that costs anything is the same: one crossing, bases once per chunk, one
/// delegate invocation per entity.
/// </remarks>
internal delegate void V0Body<T1>(ref T1 c1)
    where T1 : struct;

internal delegate void V0Body<T1, T2, T3, T4, T5>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
    where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;
