using System.ComponentModel;
using Friflo.Engine.ECS;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Entity;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct EntityHandle(RawEntity rawEntity, IEntityApi api)
{
    public bool IsAlive() => api.IsAlive(rawEntity);

    public bool HasComponent<T>() where T : struct, IComponent
    {
        return api.HasComponent<T>(rawEntity);
    }

    public ref T GetComponent<T>() where T : struct, IComponent
    {
        return ref api.GetComponent<T>(rawEntity);
    }

    public bool TryGetComponent<T>(out T component) where T : struct, IComponent
    {
        return api.TryGetComponent(rawEntity, out component);
    }

    public void AddComponent<T>() where T : struct, IComponent
    {
        api.AddComponent<T>(rawEntity);
    }

    public bool HasTag<T>() where T : struct, ITag
    {
        return api.HasTag<T>(rawEntity);
    }

    public void SetTag<T>(bool set) where T : struct, ITag
    {
        api.SetTag<T>(rawEntity, set);
    }
}
