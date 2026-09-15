using Friflo.Engine.ECS;
using FrifloEntity = Friflo.Engine.ECS.Entity;
using ReadyM.SDK.Archetypes.Attributes;

namespace ReadyM.SDK.Benchmarks;

[ArchetypeMixin]
public readonly partial struct Vitality
{
    public partial float Hp { get; set; }
    public partial float MaxHp { get; set; }
}

[Archetype]
[Include(typeof(Vitality))]
public readonly partial struct Creep
{
    public partial int Level { get; set; }
}

/// <summary>
/// What a v0 entity wrapper looks like: a struct over a Friflo entity, reading the component type
/// directly. Written by hand here because the real ones live in the game SDKs, not in the core.
/// </summary>
public readonly struct V0Creep(FrifloEntity entity)
{
    public float Hp
    {
        get => entity.GetComponent<VitalityComponent>().hp;
        set => entity.GetComponent<VitalityComponent>().hp = value;
    }

    public float MaxHp => entity.GetComponent<VitalityComponent>().maxHp;

    public int Level => entity.GetComponent<CreepComponent>().level;
}

// Five mixins, so an entity's data is spread across five components the way a real archetype's is.

[ArchetypeMixin]
public readonly partial struct Stamina
{
    public partial float Endurance { get; set; }
}

[ArchetypeMixin]
public readonly partial struct Armor
{
    public partial int Rating { get; set; }
}

[ArchetypeMixin]
public readonly partial struct Speed
{
    public partial float Pace { get; set; }
}

[ArchetypeMixin]
public readonly partial struct Purse
{
    public partial int Gold { get; set; }
}

[ArchetypeMixin]
public readonly partial struct Morale
{
    public partial float Spirit { get; set; }
}

[Archetype]
[Include(typeof(Stamina))]
[Include(typeof(Armor))]
[Include(typeof(Speed))]
[Include(typeof(Purse))]
[Include(typeof(Morale))]
public readonly partial struct Hero;

/// The v0 shape over five components, as the control for the five-component comparison.
public readonly struct V0Hero(FrifloEntity entity)
{
    public float Endurance => entity.GetComponent<StaminaComponent>().endurance;
    public int Rating => entity.GetComponent<ArmorComponent>().rating;
    public float Pace => entity.GetComponent<SpeedComponent>().pace;
    public int Gold => entity.GetComponent<PurseComponent>().gold;
    public float Spirit => entity.GetComponent<MoraleComponent>().spirit;
}

/// <summary>
/// The v1 path with the interface taken out: same handle-plus-api shape, but the api is the concrete
/// type, so the generic call can devirtualize. Splits "interface dispatch" from "extra node lookup".
/// </summary>
internal readonly struct ConcreteApiHero(RawEntity raw, ReadyM.SDK.Client.Entity.ClientEntityApi api)
{
    public float Endurance => api.GetComponent<StaminaComponent>(raw).endurance;
    public int Rating => api.GetComponent<ArmorComponent>(raw).rating;
    public float Pace => api.GetComponent<SpeedComponent>(raw).pace;
    public int Gold => api.GetComponent<PurseComponent>(raw).gold;
    public float Spirit => api.GetComponent<MoraleComponent>(raw).spirit;
}

/// <summary>
/// The v1 path with the api taken out entirely: resolve the entity once, then read each component.
/// This is what per-entity resolution caching would amount to.
/// </summary>
public readonly struct ResolvedHero(FrifloEntity entity)
{
    public float Endurance => entity.GetComponent<StaminaComponent>().endurance;
    public int Rating => entity.GetComponent<ArmorComponent>().rating;
    public float Pace => entity.GetComponent<SpeedComponent>().pace;
    public int Gold => entity.GetComponent<PurseComponent>().gold;
    public float Spirit => entity.GetComponent<MoraleComponent>().spirit;
}
