using System.ComponentModel;
using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Exceptions;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Entity;

/// <summary>
/// What an archetype or mixin struct holds: which entity it is, and how to reach its components.
/// Operations are delegated to <see cref="IEntityApi"/> so that the server and the client can implement them differently.
/// </summary>
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

    internal RawEntity RawEntity => _rawEntity;

    /// Names the entity in a message without putting its raw id on the public surface.
    public override string ToString()
        => $"entity {_rawEntity.Id}";

    public bool IsAlive()
        => _api.IsAlive(_rawEntity);

    public bool Is<T>() where T : struct, IArchetypeQueryable
        => _api.HasComponents(_rawEntity, default(T).Components);

    public T As<T>() where T : struct, IArchetypeQueryable
    {
        return Is<T>() ? new T { Handle = this } : throw new InvalidEntityException($"Cannot cast entity to type {typeof(T).Name}");
    }

    public bool TryAs<T>(out T archetype) where T : struct, IArchetypeQueryable
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
        => Locate<T>().Found;

    public ref T GetComponent<T>() where T : struct, IComponent
    {
        var located = Locate<T>();

        if (!located.Found)
            throw Missing<T>();

        return ref located.As<T>();
    }

    public bool TryGetComponent<T>(out T component) where T : struct, IComponent
    {
        var located = Locate<T>();

        if (!located.Found)
        {
            component = default;
            return false;
        }

        component = located.As<T>();
        return true;
    }

    public void AddComponent<T>() where T : struct, IComponent
        => _api.AddComponent<T>(_rawEntity);

    /// Writes the whole component, which is what moves it in the index kept on it.
    public void ReplaceIndexed<TComponent, TKey>(in TComponent component)
        where TComponent : struct, IIndexedComponent<TKey>
        => _api.ReplaceIndexed<TComponent, TKey>(_rawEntity, component);

    /// The handle behind an archetype or mixin struct.
    public static EntityHandle Of<T>(in T archetype)
        where T : struct, IArchetypeQueryable
        => archetype.Handle;

    /// <summary>The same reach into the world, pointed at another entity.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public EntityHandle For(RawEntity entity)
        => new(entity, _api);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ComponentRef Locate<T>() where T : struct, IComponent
        => _api.Locate(_rawEntity, ComponentIds<T>.For(_api));

    private ComponentNotFoundException Missing<T>()
        => new($"Entity {_rawEntity.Id} is gone or does not carry {typeof(T).Name}.");
}