using BenchmarkDotNet.Attributes;
using Friflo.Engine.ECS;
using FrifloEntity = Friflo.Engine.ECS.Entity;
using ReadyM.SDK.Client.Entity;
using SdkEntities = ReadyM.SDK.Client.Entity.Entities;

namespace ReadyM.SDK.Benchmarks;

/// One world of creeps, shared by every benchmark class.
[MemoryDiagnoser]
[ShortRunJob]
public abstract class BenchmarkWorld
{
    [Params(10_000)]
    public int Entities { get; set; }

    protected EntityStore Store = null!;
    protected IEntities Sdk = null!;
    internal ArchetypeQuery<VitalityComponent> Friflo = null!;
    internal ForEachEntity<VitalityComponent> ReadLambda = null!;
    internal ForEachEntity<VitalityComponent> RegenLambda = null!;
    protected float Sink;

    [GlobalSetup]
    public void Setup()
    {
        Store = new EntityStore();

        for (var i = 0; i < Entities; i++)
            Store.CreateEntity(
                new CreepComponent { level = i },
                new VitalityComponent { hp = 1f, maxHp = 100f });

        Sdk = new SdkEntities(Store, new ClientEntityApi(Store));
        Friflo = Store.Query<VitalityComponent>();

        ReadLambda = (ref vitality, _) => Sink += vitality.hp;

        RegenLambda = (ref vitality, _) =>
        {
            if (vitality.hp < vitality.maxHp)
                vitality.hp += 1f;

            Sink += vitality.hp;
        };
    }
}
