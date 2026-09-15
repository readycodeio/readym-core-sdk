using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[Archetype]
[IncludeArchetype(typeof(ReadyObject))] // include another archetype: its mixins, tags, and a conversion
[Include(typeof(Vitals))] // include a mixin: its accessors appear directly on MainCharacter
[Include(typeof(Equipment), Optional: true)] // optional mixin: read-only nullable accessors (below)
// [Include(typeof(Transform), Prefix: "Spawn")]     // prefixed include: SpawnPosition, SpawnRotation
public readonly partial struct MainCharacter
{
    public partial PlayerId PlayerId { get; } // accessor declared directly: goes into the default component
}

#region Generated

internal struct MainCharacterComponent : IComponent
{
    public readonly PlayerId playerId;
}

public readonly partial struct MainCharacter : IArchetype
{
    private readonly EntityHandle _handle;

    public MainCharacter(EntityHandle handle)
    {
        _handle = handle;
    }

    EntityHandle IArchetypeQueryable.Handle
    {
        get  => _handle;
        init  => _handle = value;
    }

    public ComponentSet Components => ComponentSet.Of<MainCharacterComponent, VitalsComponent>();

    public bool IsValid => _handle.IsAlive();

    public bool Has<T>() where T : struct, ITag => _handle.HasTag<T>();

    public void Set<T>(bool set) where T : struct, ITag
    {
        _handle.SetTag<T>(set);
    }

    // own component

    public partial PlayerId PlayerId => _handle.GetComponent<MainCharacterComponent>().playerId;

    // end own component

    // ReadyObject

    public static implicit operator ReadyObject(MainCharacter d)
    {
        return new ReadyObject(d._handle);
    }

    // end ReadyObject

    // vitals

    public float Hp
    {
        get => _handle.GetComponent<VitalsComponent>().hp;
        set => _handle.GetComponent<VitalsComponent>().hp = value;
    }

    public float MaxHp
    {
        get => _handle.GetComponent<VitalsComponent>().maxHp;
        set => _handle.GetComponent<VitalsComponent>().maxHp = value;
    }

    public bool IsDead => _handle.GetComponent<VitalsComponent>().isDead;

    // end vitals

    // equipment

    public string? Weapon
    {
        get
        {
            if (_handle.TryGetComponent<EquipmentComponent>(out var component))
            {
                return component.weapon;
            }

            return null;
        }
    }

    public bool TryGetEquipment(out Equipment equipment)
    {
        if (_handle.HasComponent<EquipmentComponent>())
        {
            equipment = new Equipment(_handle);
            return true;
        }

        equipment = default;
        return false;
    }

    public Equipment RequireEquipment()
    {
        if (!_handle.HasComponent<EquipmentComponent>())
            throw new Exception("Component not found");

        return new Equipment(_handle);
    }

    // TODO: This is a structural change
    public Equipment EnsureEquipment()
    {
        if (!_handle.HasComponent<EquipmentComponent>())
        {
            _handle.AddComponent<EquipmentComponent>();
        }

        return new Equipment(_handle);
    }

    // end equipment 
}

#endregion