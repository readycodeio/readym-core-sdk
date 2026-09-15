using System.ComponentModel;
using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Entity;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct EntityHandle
{
    private readonly RawEntity _rawEntity;
    private readonly IEntityApi _api;
    
    internal EntityHandle(RawEntity rawEntity, IEntityApi api)
    {
        _rawEntity = rawEntity;
        _api = api;
    }
    
    internal int Id => _rawEntity.Id;

    /// <summary>Identity including the revision, which tells this entity apart from a later one that took its id.</summary>
    internal RawEntity RawEntity => _rawEntity;

    /// <summary>
    /// The handle behind an archetype or mixin struct, without boxing it. Generated conversions read
    /// it out of the struct they convert from.
    /// </summary>
    public static EntityHandle Of<T>(in T archetype) where T : struct, IArchetypeQueryable => archetype.Handle;

    public override string ToString() => $"entity {_rawEntity.Id}";

    public bool IsAlive() => _api.IsAlive(_rawEntity);

    public bool Is<T>() where T : struct, IArchetype => _api.HasComponents(_rawEntity, default(T).Components);

    public bool TryAs<T>(out T archetype) where T : struct, IArchetype
    {
        if (Is<T>())
        {
            archetype = new T { Handle = this };
            return true;
        }

        archetype = default;
        return false;
    }

    public bool HasComponent<T>() where T : struct, IComponent
    {
        return _api.HasComponent<T>(_rawEntity);
    }

    public ref T GetComponent<T>() where T : struct, IComponent
    {
        return ref _api.GetComponent<T>(_rawEntity);
    }

    public bool TryGetComponent<T>(out T component) where T : struct, IComponent
    {
        return _api.TryGetComponent(_rawEntity, out component);
    }

    public void AddComponent<T>() where T : struct, IComponent
    {
        _api.AddComponent<T>(_rawEntity);
    }

    public bool HasTag<T>() where T : struct, ITag
    {
        return _api.HasTag<T>(_rawEntity);
    }

    public void SetTag<T>(bool set) where T : struct, ITag
    {
        _api.SetTag<T>(_rawEntity, set);
    }
}
